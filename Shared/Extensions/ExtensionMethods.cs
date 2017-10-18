namespace Shared.Extensions
{
    using System.Drawing;

    public static class ExtensionMethods
    {
        public static Rectangle Multiply(this Rectangle r, int factor)
        {
            return new Rectangle(
                r.X * factor,
                r.Y * factor,
                r.Width * factor,
                r.Height * factor);
        }

        public static Size Multiply(this Size s, int factor)
        {
            return new Size(
                s.Width * factor,
                s.Height * factor);
        }
    }
}
