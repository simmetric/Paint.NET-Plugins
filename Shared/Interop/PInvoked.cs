namespace Shared.Interop
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;

    public static class PInvoked
    {
        public static void TextOut(Graphics g, string text, int x, int y, Font font, double letterSpacing)
        {
            g.PixelOffsetMode = PixelOffsetMode.Half;
            IntPtr fontPtr = default(IntPtr);
            try
            {
                //Grab the Graphic object's handle
                IntPtr hdc = g.GetHdc();
                //Set the current GDI font
                fontPtr = Interop.SelectObject(hdc, font.ToHfont());
                //Set the drawing surface background color
                Interop.SetBkColor(hdc, ColorTranslator.ToWin32(Color.Black));
                //Set the text color
                Interop.SetTextColor(hdc, ColorTranslator.ToWin32(Color.White));
                //Set the kerning
                Interop.SetTextCharacterExtra(hdc, (int) Math.Round(letterSpacing*font.Size));
                Interop.TextOut(hdc, x, y, text, text.Length);
            }
            finally
            {
                //Release the font
                Interop.DeleteObject(fontPtr);
                //Release the handle on the graphics object
                g.ReleaseHdc();
            }
        }

#pragma warning disable S3242 // Method parameters should be declared with base types
        public static Size MeasureString(Graphics g, string text, Font font, double letterSpacing)
#pragma warning restore S3242 // Method parameters should be declared with base types
        {
            IntPtr fontPtr = default(IntPtr);
            Size size = new Size();
            try
            {
                //Grab the Graphic object's handle
                IntPtr hdc = g.GetHdc();
                //Set the current GDI font
                fontPtr = Interop.SelectObject(hdc, font.ToHfont());
                //Set the kerning
                Interop.SetTextCharacterExtra(hdc, (int) Math.Round(letterSpacing*font.Size));
                Interop.GetTextExtentPoint(hdc, text, text.Length, ref size);
            }
            finally
            {
                //Release the font
                Interop.DeleteObject(fontPtr);
                //Release the handle on the graphics object
                g.ReleaseHdc();
            }
            return size;
        }
    }
}
