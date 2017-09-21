namespace SpacedTextPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;
    using System.Linq;
    using PaintDotNet;
    using C = Constants;

    internal class SpacedText : IDisposable
    {
        //configuration options
        public string Text { get; set; }
        public FontFamily FontFamily { get; set; }
        public int FontSize { get; set; }
        public double LetterSpacing { get; set; }
        public double LineSpacing { get; set; }
        public int AntiAliasLevel { get; set; }
        public FontStyle FontStyle { get; set; }
        public C.TextAlignmentOptions TextAlign { get; set; }

        //flow control
        public bool IsCancelRequested { get; set; }

        //public result
        public PdnRegion SelectionRegion { get; private set; }
        public Rectangle Bounds { get; private set; }
        public Surface BufferSurface { get; private set; }

        //private
        private readonly ImageAttributes imgAttr;

        public SpacedText()
        {
            AntiAliasLevel = C.DefaultAntiAliasingLevel;

            ColorMap[] colorMap = {
                new ColorMap
                {
                    OldColor = Color.Black,
                    NewColor = Color.Transparent
                }
            };
            imgAttr = new ImageAttributes();
            imgAttr.SetRemapTable(colorMap);
        }

        public void RenderText(PdnRegion selection)
        {
            try
            {
                SelectionRegion = selection;
                Bounds = selection.GetBoundsInt();
                Font font = new Font(FontFamily, FontSize, FontStyle, GraphicsUnit.Pixel);

                //render text on larger bitmap so it can be anti-aliased while scaling down
                Rectangle upscaledBounds = Bounds.Multiply(AntiAliasLevel);
                Bitmap bm = new Bitmap(upscaledBounds.Width, upscaledBounds.Height);
                Graphics gr = Graphics.FromImage(bm);
                gr.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.CompositingMode = CompositingMode.SourceOver;
                gr.Clear(Color.Black);

                if (!IsCancelRequested)
                {
                    //split in lines
                    List<LineInfo> lines = LineWrap(gr, font);

                    if (!IsCancelRequested)
                    {
                        //draw lines
                        DrawLines(lines, gr, bm);
                    }
                }

                //scale bitmap down onto result-size bitmap and apply anti-aliasing
                Bitmap resultBm = new Bitmap(Bounds.Width, Bounds.Height);
                Graphics resultGr = Graphics.FromImage(resultBm);
                resultGr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                resultGr.DrawImage(bm, 0f, 0f, Bounds.Width, Bounds.Height);
                BufferSurface = Surface.CopyFromBitmap(resultBm);
                
                //cleanup
                gr.Dispose();
                bm.Dispose();
                resultGr.Dispose();
                resultBm.Dispose();
            }
            catch (OutOfMemoryException)
            {
                //scale back anti-aliasing
                if (AntiAliasLevel > 1)
                {
                    AntiAliasLevel--;
                }
            }
        }

        private void DrawLines(List<LineInfo> lines, Graphics gr, Bitmap bm)
        {
            Font font = new Font(FontFamily, FontSize * AntiAliasLevel, FontStyle, GraphicsUnit.Pixel);

            foreach (LineInfo line in lines)
            {
                var lineBounds = line.LineBounds.Multiply(AntiAliasLevel);
                var textSize = line.TextSize.Multiply(AntiAliasLevel);

                if (IsCancelRequested || line.LineBounds.Top > Bounds.Bottom * AntiAliasLevel)
                {
                    break;
                }
                
                if (!string.IsNullOrWhiteSpace(line.Text))
                {
                    int left = (line.LineBounds.Left) + ((FontSize * AntiAliasLevel) / 2);

                    if (TextAlign != C.TextAlignmentOptions.Justify)
                    {
                        if (line.TextSize.Width > 0 && line.TextSize.Height > 0 &&
                            textSize.Width < C.MaxBitmapSize &&
                            textSize.Height < C.MaxBitmapSize)
                        {
                            //create new bitmap for line
                            Bitmap lineBm = new Bitmap(textSize.Width, textSize.Height + (int)font.Size);
                            Graphics lineGr = Graphics.FromImage(lineBm);
                            //draw text
                            PInvoked.TextOut(lineGr, line.Text, 0, 0, font, LetterSpacing);

#if DEBUG
                            lineBm.Save("C:\\dev\\line" + lines.IndexOf(line) + ".png", ImageFormat.Png);
#endif
                            
                            //apply alignment: determine horizontal start position
                            if (TextAlign == C.TextAlignmentOptions.Center)
                            {
                                left = line.LineBounds.Left + (line.LineBounds.Width / 2 - line.TextSize.Width / 2);
                            }
                            else if (TextAlign == C.TextAlignmentOptions.Right)
                            {
                                left = line.LineBounds.Left + (line.LineBounds.Width - (line.TextSize.Width + (FontSize)));
                            }

                            //draw lineBm to bm leaving out black
                            gr.DrawImage(lineBm,
                                new Rectangle(
                                    left,
                                    lineBounds.Top,
                                    lineBm.Width,
                                    lineBm.Height
                                ),
                                0, 0,
                                lineBm.Width,
                                lineBm.Height, GraphicsUnit.Pixel, imgAttr);
                            lineGr.Dispose();
                            lineBm.Dispose();

#if DEBUG
                            bm.Save("C:\\dev\\bm.png", ImageFormat.Png);
#endif
                        }
                    }
                    else
                    {
                        //measure text without spaces
                        string lineWithoutSpaces = line.Text.Replace(" ", string.Empty);
                        Size textBounds = PInvoked.MeasureString(gr, lineWithoutSpaces, font, LetterSpacing);
                        
                        //calculate width of spaces
                        int spaceWidth = FontSize * AntiAliasLevel;
                        if (textBounds.Width > lineBounds.Width / 2)
                        {
                            spaceWidth = (lineBounds.Width - textBounds.Width - FontSize * AntiAliasLevel) /
                                         Math.Max(line.Text.Length - lineWithoutSpaces.Length, 1);
                        }

                        //create new bitmap for line
                        Bitmap lineBm = new Bitmap(lineBounds.Width, textBounds.Height);
                        Graphics lineGr = Graphics.FromImage(lineBm);

                        //draw word by word with correct space in between
                        foreach (string word in line.Text.Split(' '))
                        {
                            //draw text
                            PInvoked.TextOut(lineGr, word, left, 0, font, LetterSpacing);

                            Size wordBounds = PInvoked.MeasureString(lineGr, word, font, LetterSpacing);
                            left += wordBounds.Width + spaceWidth;
                        }

                        //draw lineBm to bm leaving out black
                        gr.DrawImage(lineBm, 
                            new Rectangle(
                                new Point(0, lineBounds.Top), 
                                new Size(lineBounds.Width, textBounds.Height)), 
                            0, 0,
                            lineBm.Width,
                            lineBm.Height, GraphicsUnit.Pixel, imgAttr);
                        lineGr.Dispose();
                        lineBm.Dispose();
                    }
                }
            }
        }

        private List<LineInfo> LineWrap(Graphics gr, Font font)
        {
            string[] words = Text.Replace(Environment.NewLine, " " + Environment.NewLine + C.Space)
                .Split(new[] {C.SpaceChar}, StringSplitOptions.RemoveEmptyEntries);

            List<LineInfo> lines = new List<LineInfo>();

            int y = 0;
            string currentLine = words.Any() ? words.First() + C.Space : string.Empty;

            Rectangle currentLineBounds = DetermineLineBounds(TraceLineExtent(y), TraceLineExtent(y + font.Height));

            foreach (string word in words.Skip(1))
            {
                //if manual line break: end current line and start new line
                if (word.Equals(Environment.NewLine))
                {
                    lines.Add(new LineInfo
                    {
                        Text = currentLine.Trim(),
                        LineBounds = currentLineBounds,
                        TextSize = new Size(1, font.Height)
                    });
                    y += (int)Math.Round(font.Height * 1.5) + (int)Math.Round(font.Height * LineSpacing);
                    currentLine = string.Empty;
                    continue;
                }

                //measure currentline + word
                //else add word to currentline
                Size textBounds = PInvoked.MeasureString(gr, currentLine + word, font, LetterSpacing);
                if (textBounds.Width > (currentLineBounds.Width) - FontSize)
                {
                    //if outside bounds, then add line
                    lines.Add(new LineInfo
                    {
                        Text = currentLine.Trim(),
                        LineBounds = currentLineBounds,
                        TextSize = textBounds
                    });
                    y += font.Height + (int)Math.Round(font.Height * LineSpacing);
                    currentLine = word + C.Space;
                    currentLineBounds = DetermineLineBounds(TraceLineExtent(y), TraceLineExtent(y + font.Height));
                }
                else
                {
                    currentLine += word + C.Space;
                }
            }
            //add currentline
            if (!string.IsNullOrEmpty(currentLine))
            {
                Size textBounds = PInvoked.MeasureString(gr, currentLine, font, LetterSpacing);
                lines.Add(new LineInfo
                {
                    Text = currentLine.Trim(),
                    LineBounds = currentLineBounds,
                    TextSize = textBounds
                });
            }
            return lines;
        }

        private Rectangle DetermineLineBounds(Extent topLine, Extent bottomLine)
        {
            int maxLeftX = Math.Max(topLine.Left, bottomLine.Left);
            int minRightX = Math.Min(topLine.Right, bottomLine.Right);
            return new Rectangle(maxLeftX, topLine.VerticalPosition, minRightX-maxLeftX, (bottomLine.VerticalPosition-topLine.VerticalPosition) + (int)Math.Round(FontSize * 1.5));
        }

        /// <summary>
        /// Returns the left and right visible extents of the current line in relation to the selection bounds
        /// </summary>
        /// <param name="lineY"></param>
        /// <returns></returns>
        private Extent TraceLineExtent(int lineY)
        {
            int leftX = Bounds.Left;
            int rightX = Bounds.Right;

            lineY += Bounds.Top;

            //trace line inward from left to right
            while (leftX < rightX)
            {
                if (SelectionRegion.IsVisible(leftX, lineY))
                {
                    break;
                }

                leftX++;
            }

            while (rightX > 0)
            {
                if (SelectionRegion.IsVisible(rightX, lineY))
                {
                    break;
                }

                rightX--;
            }

            return new Extent(leftX - Bounds.Left, rightX - Bounds.Left, lineY - Bounds.Top);
        }

        public void Dispose()
        {
            BufferSurface.Dispose();
        }
    }
}
