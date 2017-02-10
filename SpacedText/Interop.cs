using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpacedTextPlugin
{
    using System.Runtime.InteropServices;

    internal class Interop
    {
        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern int SetTextCharacterExtra(IntPtr hdc, int nCharExtra);

        [DllImport("gdi32")]
        public static extern bool TextOut(IntPtr hdc, int x, int y, string textstring, int charCount);
        
        [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
        public static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

        [DllImport("gdi32")]
        public static extern bool DeleteObject(IntPtr objectHandle);

        [DllImport("gdi32")]
        public static extern UInt32 SetTextColor(IntPtr hdc, int crColor);

        [DllImport("gdi32")]
        public static extern UInt32 SetBkColor(IntPtr hdc, int crColor);
    }
}