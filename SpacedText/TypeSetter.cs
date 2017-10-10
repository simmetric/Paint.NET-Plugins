namespace SpacedTextPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Text;
    using PaintDotNet;
    using SpacedTextPlugin.Data;
    using SpacedTextPlugin.Interop;

    internal class TypeSetter : IDisposable
    {
        //parameters
        private readonly Settings settings;
        private readonly PdnRegion selectionRegion;
        private readonly Rectangle bounds;

        //drawing tools for measuring
        private readonly Font font;
        private readonly Image image;
        private readonly Graphics graphics;
        private readonly Rectangle scaledBounds;

        //constants
        private readonly int lineIncrement;

        private readonly int horizontalMargin;

        //result
        public ICollection<LineData> Lines { get; }

        public TypeSetter(Settings settings, PdnRegion selectionRegion)
        {
            //parameters in 
            this.settings = settings;
            this.selectionRegion = selectionRegion;
            bounds = selectionRegion.GetBoundsInt();

            //convert to AA space
            scaledBounds = bounds.Multiply(settings.AntiAliasLevel);
            font = settings.GetAntiAliasSizeFont();
            image = new Bitmap(scaledBounds.Width, scaledBounds.Height);
            graphics = Graphics.FromImage(image);

            //constants
            lineIncrement = (int) Math.Round(font.Height + font.Height * settings.LineSpacing);
            horizontalMargin = (int)Math.Round(font.Size / 10f);

            Lines = new List<LineData>();
        }

        public void SetText()
        {
            IEnumerable<string> words = settings.Text.Replace(Environment.NewLine, Constants.Space + Environment.NewLine + Constants.Space).Split(' ');
            StringBuilder currentLineText = new StringBuilder();
            int y = 0;
            LineData currentLine = StartNewLine(ref y);

            //until all words are placed or y is outside scaledBounds
            foreach (string word in words)
            {
                if (y > scaledBounds.Bottom + lineIncrement)
                {
                    break;
                }

                if (word.Equals(Environment.NewLine))
                {
                    EndLine(currentLine, currentLineText);

                    //start new line
                    currentLineText.Clear();
                    currentLine = StartNewLine(ref y);

                    continue;
                }

                Size textSize = PInvoked.MeasureString(graphics, (currentLineText + word).Trim(), font,
                    settings.LetterSpacing);
                if (textSize.Width > currentLine.LineBounds.Width)
                {
                    EndLine(currentLine, currentLineText);

                    //start new line
                    currentLineText.Clear();
                    currentLine = StartNewLine(ref y);
                    currentLineText.Append(word + Constants.Space);
                }
                else
                {
                    currentLineText.Append(word + Constants.Space);
                }
            }
            if (currentLine != null && currentLineText.Length > 0)
            {
                EndLine(currentLine, currentLineText);
            }
        }

        public void AlignText()
        {
            foreach (var line in Lines)
            {
                var lineBounds = line.LineBounds;

                switch (settings.TextAlign)
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

        private LineData StartNewLine(ref int y)
        {
            //find line extents
            LineData newLine = new LineData
            {
                LineBounds = DetermineLineBounds(
                    TraceLineExtent(y),
                    TraceLineExtent(y + font.Height))
            };
            y += lineIncrement;

            return newLine;
        }

        private void EndLine(LineData currentLine, StringBuilder currentLineText)
        {
            currentLine.Text = currentLineText.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(currentLine.Text))
            {
                currentLine.TextSize = PInvoked.MeasureString(graphics, currentLine.Text, font,
                    settings.LetterSpacing);
                Lines.Add(currentLine);
            }
        }

        private Rectangle DetermineLineBounds(Extent topLine, Extent bottomLine)
        {
            int maxLeftX = Math.Max(topLine.Left, bottomLine.Left) + horizontalMargin;
            int minRightX = Math.Max(0, Math.Min(topLine.Right, bottomLine.Right) - horizontalMargin);
            return new Rectangle(maxLeftX, topLine.VerticalPosition, Math.Max(1, (minRightX - maxLeftX)),
                (bottomLine.VerticalPosition - topLine.VerticalPosition));
        }

        /// <summary>
        /// Returns the left and right visible extents of the current line in relation to the selection scaledBounds
        /// </summary>
        /// <param name="scaledLineY">The y coordinate in AA scale</param>
        /// <returns>The visible extent of the line in AA scale-coordinates</returns>
        private Extent TraceLineExtent(int scaledLineY)
        {

            int lineY = (scaledLineY / settings.AntiAliasLevel) + bounds.Top;

            //if y falls outside selection, simply return entire line
            if (lineY < bounds.Top || lineY > bounds.Bottom)
            {
                return new Extent(bounds.Left, bounds.Right, lineY - bounds.Top).Multiply(settings.AntiAliasLevel);
            }

            int leftX = bounds.Left;
            int rightX = bounds.Right;
            //trace line inward from left to right
            while (leftX < rightX)
            {
                if (selectionRegion.IsVisible(leftX, lineY))
                {
                    break;
                }

                leftX++;
            }

            while (rightX > 0)
            {
                if (selectionRegion.IsVisible(rightX, lineY))
                {
                    break;
                }

                rightX--;
            }

            return new Extent(leftX - bounds.Left, rightX - bounds.Left, lineY - bounds.Top).Multiply(settings.AntiAliasLevel);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            image.Dispose();
            graphics.Dispose();
            font.Dispose();
        }
    }
}
