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

        public void RenderText(Rectangle bounds)
        {
            try
            {
                Bounds = bounds;
                Font font = new Font(FontFamily, FontSize*AntiAliasLevel, FontStyle, GraphicsUnit.Pixel);

                //render text on larger bitmap so it can be anti-aliased while scaling down
                Bitmap bm = new Bitmap(Bounds.Size.Width*AntiAliasLevel, Bounds.Size.Height*AntiAliasLevel);
                Graphics gr = Graphics.FromImage(bm);
                gr.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.CompositingMode = CompositingMode.SourceOver;
                gr.Clear(Color.Black);

                //letterspacing may be changed during execution
                double letterSpacing = LetterSpacing;

                if (!IsCancelRequested)
                {
                    //split in lines
                    List<string> lines = LineWrap(gr, font, letterSpacing, bm);

                    if (!IsCancelRequested)
                    {
                        //draw lines
                        DrawLines(lines, gr, font, letterSpacing, bm);
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

        private void DrawLines(List<string> lines, Graphics gr, Font font, double letterSpacing, Bitmap bm)
        {
            int lineStart = 0;
            foreach (string line in lines)
            {
                if (IsCancelRequested || lineStart > Bounds.Bottom * AntiAliasLevel)
                {
                    break;
                }
                
                if (!string.IsNullOrWhiteSpace(line))
                {
                    int left = FontSize / 2;

                    if (TextAlign != C.TextAlignmentOptions.Justify)
                    {
                        //measure text
                        Size textBounds = PInvoked.MeasureString(gr, line, font, letterSpacing);
                        if (TextAlign == C.TextAlignmentOptions.Center)
                        {
                            left = bm.Width / 2 - textBounds.Width / 2;
                        }
                        else if (TextAlign == C.TextAlignmentOptions.Right)
                        {
                            left = bm.Width - (textBounds.Width + FontSize);
                        }

                        if (textBounds.Width > 0 && textBounds.Height > 0 &&
                            textBounds.Width * AntiAliasLevel < C.MaxBitmapSize &&
                            textBounds.Height * AntiAliasLevel < C.MaxBitmapSize)
                        {
                            //create new bitmap for line
                            Bitmap lineBm = new Bitmap(textBounds.Width * AntiAliasLevel,
                                textBounds.Height * AntiAliasLevel);
                            Graphics lineGr = Graphics.FromImage(lineBm);
                            //draw text
                            PInvoked.TextOut(lineGr, line, 0, 0, font, letterSpacing);

                            //draw lineBm to bm leaving out black
                            gr.DrawImage(lineBm, new Rectangle(new Point(left, lineStart), lineBm.Size), 0, 0,
                                lineBm.Width,
                                lineBm.Height, GraphicsUnit.Pixel, imgAttr);
                            lineGr.Dispose();
                            lineBm.Dispose();
                        }
                    }
                    else
                    {
                        //measure text without spaces
                        string lineWithoutSpaces = line.Replace(" ", string.Empty);
                        Size textBounds = PInvoked.MeasureString(gr, lineWithoutSpaces, font, letterSpacing);
                        
                        //calculate width of spaces
                        int spaceWidth = FontSize;
                        if (textBounds.Width > bm.Width / 2)
                        {
                            spaceWidth = (bm.Width - textBounds.Width - FontSize) /
                                         Math.Max(line.Length - lineWithoutSpaces.Length, 1);
                        }

                        //create new bitmap for line
                        Bitmap lineBm = new Bitmap(bm.Width, bm.Height);
                        Graphics lineGr = Graphics.FromImage(lineBm);

                        //draw word by word with correct space in between.7
                        foreach (string word in line.Split(' '))
                        {
                            //draw text
                            PInvoked.TextOut(lineGr, word, left, 0, font, letterSpacing);

                            Size wordBounds = PInvoked.MeasureString(lineGr, word, font, letterSpacing);
                            left += wordBounds.Width + spaceWidth;
                        }

                        //draw lineBm to bm leaving out black
                        gr.DrawImage(lineBm, new Rectangle(new Point(0, lineStart), lineBm.Size), 0, 0,
                            lineBm.Width,
                            lineBm.Height, GraphicsUnit.Pixel, imgAttr);
                        lineGr.Dispose();
                        lineBm.Dispose();
                    }
                }

                lineStart += font.Height + (int)Math.Round(font.Height * LineSpacing);
            }
        }

        private List<string> LineWrap(Graphics gr, Font font, double letterSpacing, Bitmap bm)
        {
            string[] words = Text.Replace(Environment.NewLine, " " + Environment.NewLine + C.Space)
                .Split(new[] {C.SpaceChar}, StringSplitOptions.RemoveEmptyEntries);
            List<string> lines = new List<string>();

            string currentLine = words.Any() ? words.First() + C.Space : string.Empty;

            foreach (string word in words.Skip(1))
            {
                //if manual line break: end current line and start new line
                if (word.Equals(Environment.NewLine))
                {
                    lines.Add(currentLine.Trim());
                    currentLine = string.Empty;
                    continue;
                }

                //measure currentline + word
                //else add word to currentline
                if (PInvoked.MeasureString(gr, currentLine + word, font, letterSpacing).Width > bm.Width - FontSize)
                {
                    //if outside bounds, then add line
                    lines.Add(currentLine.Trim());
                    currentLine = word + C.Space;
                }
                else
                {
                    currentLine += word + C.Space;
                }
            }
            //add currentline
            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine.Trim());
            }
            return lines;
        }

        public void Dispose()
        {
            BufferSurface.Dispose();
        }
    }
}
