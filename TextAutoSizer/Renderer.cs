namespace TextAutoSizerPlugin
{
    using System.Drawing;
    using PaintDotNet;
    using Shared.Data;
    using Shared.Interop;
    using Settings = TextAutoSizerPlugin.Data.Settings;

    internal class Renderer : Shared.Text.Renderer
    {
        public Renderer(Settings settings, PdnRegion selectionRegion) : base(settings, selectionRegion)
        {
            
        }

        protected override void DrawLine(Graphics lineGraphics, LineData line)
        {
            settings.FontSize = line.FontSize;
            var font = settings.GetAntiAliasSizeFont();
            PInvoked.TextOut(graphics, line.Text, line.LineBounds.Left, line.LineBounds.Top, font, settings.LetterSpacing);
        }
    }
}
