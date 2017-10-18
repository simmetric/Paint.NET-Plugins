namespace SpacedTextPlugin
{
    using System.Collections.Generic;
    using System.Drawing;
    using Shared.Data;

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
    }
}
