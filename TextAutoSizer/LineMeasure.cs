namespace TextAutoSizerPlugin
{
    using System;
    using System.Drawing;
    using PaintDotNet;
    using PaintDotNet.Collections;
    using Shared.Data;
    using Shared.Interop;
    using SpacedTextPlugin;
    using Settings = TextAutoSizerPlugin.Data.Settings;

    internal class LineMeasure : TypeSetter
    {
        public LineMeasure(Settings settings, PdnRegion selectionRegion) : base(settings, selectionRegion)
        {

        }

        public override void SetText()
        {
            int y = scaledBounds.Top;

            foreach (var line in base.settings.Text
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim()))
            {
                settings.FontSize = 1;

                while (true)
                {
                    Font lineFont = settings.GetAntiAliasSizeFont();
                    var lineBounds = DetermineLineBounds(
                        TraceLineExtent(y),
                        TraceLineExtent(y + lineFont.Height));

                    Size textSize = PInvoked.MeasureString(graphics, line, lineFont, settings.LetterSpacing);

                    if (settings.FontSize < 500 && textSize.Width < lineBounds.Width)
                    {
                        settings.FontSize++;
                    }
                    else
                    {
                        Lines.Add(new LineData
                        {
                            LineBounds = lineBounds,
                            Text = line,
                            TextSize = textSize,
                            FontSize = Math.Max(settings.FontSize, 1.0f)
                        });

                        y += lineFont.Height + (int)Math.Round(settings.LineSpacing * (lineFont.Height / 2f));
                        break;
                    }
                }
            }
        }
    }
}
