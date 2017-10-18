namespace Shared.Text
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;
    using PaintDotNet;
    using Shared.Data;
    using Shared.Extensions;
    using Shared.Interop;
    
    public class Renderer : IDisposable
    {
        protected readonly Rectangle selectionBounds;
        protected readonly Font font;
        protected readonly Bitmap image;
        protected readonly Graphics graphics;
        protected readonly ImageAttributes imageAttributes;

        protected readonly Settings settings;

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

                    DrawLine(lineGraphics, line);

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

        protected virtual void DrawLine(Graphics lineGraphics, LineData line)
        {

        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

#pragma warning disable S1172 // Unused method parameters should be removed
        // ReSharper disable once UnusedParameter.Local
        private void Dispose(bool isDisposing)
#pragma warning restore S1172 // Unused method parameters should be removed
        {
            graphics.Dispose();
            image.Dispose();
            imageAttributes.Dispose();
        }

        ~Renderer()
        {
            Dispose(true);
        }
    }
}
