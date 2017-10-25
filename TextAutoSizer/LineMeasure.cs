namespace TextAutoSizerPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using PaintDotNet;
    using PaintDotNet.Collections;
    using Shared.Data;
    using Shared.Interop;
    using Shared.Text;
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
                settings.FontSize = 20;

                Font lineFont = settings.GetAntiAliasSizeFont();
                Size textSize = PInvoked.MeasureString(graphics, line, lineFont, settings.LetterSpacing);
                Rectangle lineBounds = DetermineLineBounds(
                    TraceLineExtent(y),
                    TraceLineExtent(y + lineFont.Height));

                //find target fontsize
                if (textSize.Width < lineBounds.Width)
                {
                    settings.FontSize =
                        (float) Math.Floor(
                            ((lineBounds.Width - textSize.Width) / (textSize.Width / settings.FontSize)) +
                            settings.FontSize);
                }
                else
                {
                    settings.FontSize = 1;
                }

                while (true)
                {
                    lineFont = settings.GetAntiAliasSizeFont();
                    lineBounds = DetermineLineBounds(
                        TraceLineExtent(y),
                        TraceLineExtent(y + lineFont.Height));

                    textSize = PInvoked.MeasureString(graphics, line, lineFont, settings.LetterSpacing);
                    
                    if (textSize.Width < lineBounds.Width)
                    {
                        settings.FontSize += 0.1f;
                    }
                    else
                    {
                        if (textSize.Width > lineBounds.Width)
                        {
                            settings.FontSize -= 0.1f;
                            lineFont = settings.GetAntiAliasSizeFont();
                            textSize = PInvoked.MeasureString(graphics, line, lineFont, settings.LetterSpacing);
                        }

                        Lines.Add(new LineData
                        {
                            LineBounds = lineBounds,
                            Text = line,
                            TextSize = textSize,
                            FontSize = settings.FontSize
                        });

                        y += lineFont.Height + (int)Math.Round(settings.LineSpacing * (lineFont.Height / 2f));
                        break;
                    }
                }
            }
        }
    }
}
