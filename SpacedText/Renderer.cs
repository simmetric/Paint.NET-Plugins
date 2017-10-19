namespace SpacedTextPlugin
{
    using System.Drawing;
    using PaintDotNet;
    using Shared.Data;
    using Shared.Interop;

    internal sealed class Renderer : Shared.Text.Renderer
    {
        private new readonly SpacedTextPlugin.Data.Settings settings;

        public Renderer(Data.Settings settings, PdnRegion selectionRegion) : base(settings,
            selectionRegion)
        {
            this.settings = settings;
        }

        protected override void DrawLine(Graphics lineGraphics, LineData line)
        {
            base.DrawLine(lineGraphics, line);
            if (settings.TextAlign != Constants.TextAlignmentOptions.Justify)
            {
                PInvoked.TextOut(lineGraphics, line.Text, 0, 0, font, settings.LetterSpacing);
            }
            else
            {
                TextAlignment.Justify(line, lineGraphics, settings, font);
            }
        }
    }
}
