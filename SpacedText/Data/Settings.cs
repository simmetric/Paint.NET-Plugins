namespace SpacedTextPlugin.Data
{
    using System.Drawing;

    class Settings
    {
        public string Text { get; set; }
        public FontFamily FontFamily { get; set; }
        public int FontSize { get; set; }
        public double LetterSpacing { get; set; }
        public double LineSpacing { get; set; }
        public int AntiAliasLevel { get; set; }
        public FontStyle FontStyle { get; set; }
        public Constants.TextAlignmentOptions TextAlign { get; set; }

        public Font GetFont()
        {
            return new Font(FontFamily, FontSize, FontStyle, GraphicsUnit.Pixel);
        }

        public Font GetAntiAliasSizeFont()
        {
            return new Font(FontFamily, FontSize * AntiAliasLevel, FontStyle, GraphicsUnit.Pixel);
        }
    }
}
