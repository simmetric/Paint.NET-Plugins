using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpacedTextPlugin
{
    using System.Drawing;

    public struct Extent
    {
        public int VerticalPosition { get; private set; }
        public int Left { get; private set; }
        public int Right { get; private set; }
        public int Width { get; private set; }

        public Point Start
        {
            get
            {
                return new Point(Left, VerticalPosition);
            }
        }

        public Point End
        {
            get
            {
                return new Point(Right, VerticalPosition);
            }
        }

        public Extent(int left, int right, int y)
        {
            Left = left;
            Right = right;
            Width = right - left;
            VerticalPosition = y;
        }
    }
}
