﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpacedTextPlugin
{
    using System.Drawing;
    using System.Runtime.InteropServices;

    public class PInvoked
    {
        public static void TextOut(Graphics G, string text, int x, int y, Font font, float letterSpacing)
        {
            //If you want kerning
            //I think this is twips
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
                Interop.SetTextCharacterExtra(Hdc, (int) (letterSpacing*font.Size));
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
    }
}
