namespace TextAutoSizerPlugin
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using PaintDotNet;
    using SpacedTextPlugin;

    internal class TextAutoSizer : IDisposable
    {
        //flow control
        public bool IsCancelRequested { get; set; }

        //public result
        public Rectangle Bounds { get; private set; }

        public Surface BufferSurface { get; private set; }

        public void RenderText(PdnRegion selection, Data.Settings settings)
        {
            var selectionRegion = selection;
            Bounds = selectionRegion.GetBoundsInt();

            try
            {
                LineMeasure measure = new LineMeasure(settings, selectionRegion);
                measure.SetText();

                Renderer renderer = new Renderer(settings, selectionRegion);
                Bitmap resultImage = renderer.Draw(measure.Lines);
#if DEBUG
                resultImage.Save("C:\\dev\\redo_bm.png", ImageFormat.Png);
#endif

                BufferSurface = Surface.CopyFromBitmap(resultImage);
            }
            catch (OutOfMemoryException)
            {
                //scale back anti-aliasing
                if (settings.AntiAliasLevel > 1)
                {
                    settings.AntiAliasLevel--;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            BufferSurface.Dispose();
        }
    }
}
