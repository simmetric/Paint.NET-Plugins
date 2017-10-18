namespace Shared.Data
{
    using System.Drawing;

    public struct Extent
    {
        public int VerticalPosition { get; private set; }
        public int Left { get; private set; }
        public int Right { get; private set; }
        public int Width { get; private set; }

        public Point Start => new Point(Left, VerticalPosition);

        public Point End => new Point(Right, VerticalPosition);

        public Extent(int left, int right, int y)
        {
            Left = left;
            Right = right;
            Width = right - left;
            VerticalPosition = y;
        }

        public Extent Multiply(int factor)
        {
            Left *= factor;
            Right *= factor;
            Width = Right - Left;
            VerticalPosition *= factor;

            return this;
        }
    }
}
