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
        public string FontFamily { get; set; }
        public int FontSize { get; set; }
        public double LetterSpacing { get; set; }
        public double LineSpacing { get; set; }
        public int AntiAliasLevel { get; set; }
        public FontStyle FontStyle { get; set; }
        public Constants.TextAlignmentOptions TextAlign { get; set; }

        //flow control
        public bool IsCancelRequested { get; set; }

        //public result
        public Rectangle Bounds { get; private set; }
        public Surface BufferSurface { get; private set; }

        //private
        private readonly ImageAttributes imgAttr;

        public SpacedText()
        {
            AntiAliasLevel = Constants.DefaultAntiAliasingLevel;

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
            Bounds = bounds;
            Font font = new Font(FontFamily, FontSize * AntiAliasLevel, FontStyle, GraphicsUnit.Pixel);
            
            //render text on larger bitmap so it can be anti-aliased while scaling down
            Bitmap bm = new Bitmap(Bounds.Size.Width * AntiAliasLevel, Bounds.Size.Height * AntiAliasLevel);
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

#if DEBUG
            bm.Save("C:\\dev\\buffer.png", ImageFormat.Png);
            resultBm.Save("C:\\dev\\result.png", ImageFormat.Png);
#endif

            //cleanup
            gr.Dispose();
            bm.Dispose();
            resultGr.Dispose();
            resultBm.Dispose();
        }

        private void DrawLines(List<string> lines, Graphics gr, Font font, double letterSpacing, Bitmap bm)
        {
            int lineStart = 0;
            int lineNum = 0;
            foreach (string line in lines)
            {
                if (IsCancelRequested || lineStart > Bounds.Bottom * AntiAliasLevel)
                {
                    break;
                }

                lineNum++;

                if (!line.Equals(string.Empty))
                {
                    int left = FontSize / 2;

                    //measure text
                    Size textBounds = PInvoked.MeasureString(gr, line, font, letterSpacing);
                    if (TextAlign != Constants.TextAlignmentOptions.Left)
                    {
                        if (TextAlign == Constants.TextAlignmentOptions.Center)
                        {
                            left = bm.Width / 2 - textBounds.Width / 2;
                        }
                        else if (TextAlign == Constants.TextAlignmentOptions.Right)
                        {
                            left = bm.Width - (textBounds.Width + FontSize);
                        }
                    }

                    if (textBounds.Width > 0 && textBounds.Height > 0 && 
                        textBounds.Width * AntiAliasLevel < Constants.MaxBitmapSize && textBounds.Height * AntiAliasLevel < Constants.MaxBitmapSize)
                    {
                        //create new bitmap for line
                        Bitmap lineBm = new Bitmap(textBounds.Width * AntiAliasLevel, textBounds.Height * AntiAliasLevel);
                        Graphics lineGr = Graphics.FromImage(lineBm);
                        //draw text
                        PInvoked.TextOut(lineGr, line, 0, 0, font, letterSpacing);
#if DEBUG
                        lineBm.Save($"C:\\dev\\line{lineNum}.png", ImageFormat.Png);
#endif
                        //draw lineBm to bm leaving out black
                        gr.DrawImage(lineBm, new Rectangle(new Point(left, lineStart), lineBm.Size), 0, 0, lineBm.Width,
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
