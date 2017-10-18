namespace SpacedTextPlugin
{
    using System;
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
                Justify(line, lineGraphics);
            }
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
    }
}
