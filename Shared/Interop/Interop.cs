namespace Shared.Interop
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;

    public static class Interop
    {
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        public static extern int SetTextCharacterExtra(IntPtr hdc, int nCharExtra);

        [DllImport("gdi32", CharSet = CharSet.Unicode)]
        public static extern bool TextOut(IntPtr hdc, int x, int y, string textstring, int charCount);
        
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "SelectObject")]
        public static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

        [DllImport("gdi32", CharSet = CharSet.Unicode)]
        public static extern bool DeleteObject(IntPtr objectHandle);

        [DllImport("gdi32", CharSet = CharSet.Unicode)]
        public static extern uint SetTextColor(IntPtr hdc, int crColor);

        [DllImport("gdi32", CharSet = CharSet.Unicode)]
        public static extern uint SetBkColor(IntPtr hdc, int crColor);

        [DllImport("gdi32", CharSet = CharSet.Unicode)]
        public static extern void GetTextExtentPoint(IntPtr hdc, string text, int charcount, ref Size size);
    }
}