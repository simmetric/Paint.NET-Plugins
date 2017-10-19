namespace SpacedTextPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Shared.Data;
    using Shared.Interop;
    using Settings = SpacedTextPlugin.Data.Settings;

    internal static class TextAlignment
    {
        public static void AlignText(IEnumerable<LineData> lines, Constants.TextAlignmentOptions textAlign)
        {
            foreach (var line in lines)
            {
                var lineBounds = line.LineBounds;

                switch (textAlign)
                {
                    //if align=center, calculate middle
                    case Constants.TextAlignmentOptions.Center:

                        lineBounds.X = line.LineBounds.Left +
                                       ((line.LineBounds.Width / 2) - (line.TextSize.Width / 2));

                        line.LineBounds = lineBounds;
                        break;

                    //if align=right, calculate left margin
                    case Constants.TextAlignmentOptions.Right:

                        lineBounds.X = line.LineBounds.Left +
                                       (line.LineBounds.Width - line.TextSize.Width);

                        line.LineBounds = lineBounds;
                        break;
                    //if align=justify, set text width = line width
                    //actual distribution of whitespace will be done in Renderer
                    case Constants.TextAlignmentOptions.Justify:
                        line.TextSize = new Size(lineBounds.Width, line.TextSize.Height);
                        break;
                }
            }
        }



        public static void Justify(LineData line, Graphics lineGraphics, Settings settings, Font font)
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
