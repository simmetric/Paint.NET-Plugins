namespace SpacedTextPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;
    using PaintDotNet;
    using SpacedTextPlugin.Data;
    using SpacedTextPlugin.Interop;
    
    internal class Renderer : IDisposable
    {
        private readonly Rectangle selectionBounds;
        private readonly Font font;
        private readonly Bitmap image;
        private readonly Graphics graphics;
        private readonly ImageAttributes imageAttributes;

        private readonly Settings settings;

        public Renderer(Settings settings, PdnRegion selectionRegion)
        {
            //convert to AA space
            selectionBounds = selectionRegion.GetBoundsInt();
            var scaledBounds = selectionBounds.Multiply(settings.AntiAliasLevel);
            font = settings.GetAntiAliasSizeFont();
            image = new Bitmap(scaledBounds.Width, scaledBounds.Height);
            graphics = Graphics.FromImage(image);
            graphics.Clear(Color.Black);
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            this.settings = settings;

            //map color black to transparent
            ColorMap[] colorMap = {
                new ColorMap
                {
                    OldColor = Color.Black,
                    NewColor = Color.Transparent
                }
            };
            imageAttributes = new ImageAttributes();
            imageAttributes.SetRemapTable(colorMap);
        }

        public Bitmap Draw(IEnumerable<LineData> lines)
        {
            foreach (var line in lines)
            {
                //create bitmap for line
                using (var lineImage = new Bitmap(line.TextSize.Width, line.TextSize.Height))
                {
                    var lineGraphics = Graphics.FromImage(lineImage);

                    if (settings.TextAlign != Constants.TextAlignmentOptions.Justify)
                    {
                        PInvoked.TextOut(lineGraphics, line.Text, 0, 0, font, settings.LetterSpacing);
                    }
                    else
                    {
                        Justify(line, lineGraphics);
                    }

                    //draw line bitmap to image
                    graphics.DrawImage(lineImage,
                        new Rectangle(
                            line.LineBounds.Location,
                            lineImage.Size
                        ), /* destination rect */
                        0, 0, /* source coordinates */
                        lineImage.Width,
                        lineImage.Height,
                        GraphicsUnit.Pixel,
                        imageAttributes
                    );

#if DEBUG
                    //draw rectangles
                    graphics.DrawRectangle(Pens.White, line.LineBounds);
                    graphics.DrawLine(Pens.Gray,
                        line.LineBounds.X,
                        line.LineBounds.Y + line.TextSize.Height / 2,
                        line.LineBounds.X + line.TextSize.Width,
                        line.LineBounds.Y + line.TextSize.Height / 2
                    );
#endif
                    lineGraphics.Dispose();
                }
            }

            //create selection-sized bitmap
            var resultImage = new Bitmap(selectionBounds.Width, selectionBounds.Height);
            var resultGraphics = Graphics.FromImage(resultImage);
            resultGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            resultGraphics.DrawImage(image, 0, 0, selectionBounds.Width, selectionBounds.Height);

            return resultImage;
        }

        private void Justify(LineData line, Graphics lineGraphics)
        {
            var lineTextWithoutSpaces = line.Text.Replace(Constants.Space, string.Empty);
            var lineSizeWithoutSpaces = PInvoked.MeasureString(lineGraphics, lineTextWithoutSpaces, font,
                settings.LetterSpacing);
            var spaceWidth = (line.TextSize.Width - lineSizeWithoutSpaces.Width) /
                             Math.Max((line.Text.Length - lineTextWithoutSpaces.Length), 1);
            if (spaceWidth > font.Size * 3)
            {
                PInvoked.TextOut(lineGraphics, line.Text, 0, 0, font, settings.LetterSpacing);
            }
            else
            {
                var x = 0;

                foreach (string word in line.Text.Split(Constants.SpaceChar))
                {
                    var wordSize = PInvoked.MeasureString(lineGraphics, word, font, settings.LetterSpacing);
                    PInvoked.TextOut(lineGraphics, word, x, 0, font, settings.LetterSpacing);
                    x += wordSize.Width + spaceWidth;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            graphics.Dispose();
            image.Dispose();
        }
    }
}
