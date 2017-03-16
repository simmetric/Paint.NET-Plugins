using System;

namespace SpacedTextPlugin
{
    using System.Drawing;
    using System.Drawing.Drawing2D;

    public class PInvoked
    {
        public static void TextOut(Graphics G, string text, int x, int y, Font font, float letterSpacing)
        {
            G.PixelOffsetMode = PixelOffsetMode.HighQuality;
            IntPtr Hdc = default(IntPtr);
            IntPtr FontPtr = default(IntPtr);
            try
            {
                //Grab the Graphic object's handle
                Hdc = G.GetHdc();
                //Set the current GDI font
                FontPtr = Interop.SelectObject(Hdc, font.ToHfont());
                //Set the drawing surface background color
                Interop.SetBkColor(Hdc, ColorTranslator.ToWin32(Color.Black));
                //Set the text color
                Interop.SetTextColor(Hdc, ColorTranslator.ToWin32(Color.White));
                //Set the kerning
                Interop.SetTextCharacterExtra(Hdc, (int) Math.Round(letterSpacing*font.Size));
                Interop.TextOut(Hdc, x, y, text, text.Length);
            }
            finally
            {
                //Release the font
                Interop.DeleteObject(FontPtr);
                //Release the handle on the graphics object
                G.ReleaseHdc();
            }
        }

        public static Size MeasureString(Graphics G, string text, Font font, float letterSpacing)
        {
            IntPtr Hdc = default(IntPtr);
            IntPtr FontPtr = default(IntPtr);
            Size size = new Size();
            try
            {
                //Grab the Graphic object's handle
                Hdc = G.GetHdc();
                //Set the current GDI font
                FontPtr = Interop.SelectObject(Hdc, font.ToHfont());
                //Set the kerning
                Interop.SetTextCharacterExtra(Hdc, (int) Math.Round(letterSpacing*font.Size));
                Interop.GetTextExtentPoint(Hdc, text, text.Length, ref size);
            }
            finally
            {
                //Release the font
                Interop.DeleteObject(FontPtr);
                //Release the handle on the graphics object
                G.ReleaseHdc();
            }
            return size;
        }
    }
}
