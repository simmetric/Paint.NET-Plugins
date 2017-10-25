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
            base.DrawLine(lineGraphics, line);

            settings.FontSize = line.FontSize;
            var font = settings.GetAntiAliasSizeFont();
            PInvoked.TextOut(lineGraphics, line.Text, 0, 0, font, settings.LetterSpacing);
        }
    }
}
