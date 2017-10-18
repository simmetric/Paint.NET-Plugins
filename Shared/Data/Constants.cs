namespace Shared.Data
{
    public static class Constants
    {
        public enum Properties
        {
            Text,
            FontSize,
            LetterSpacing,
            LineSpacing,
            AntiAliasLevel,
            FontFamily,
            Bold,
            Italic,
            Underline,
            Strikeout,
            TextAlignment
        }

        public enum TextAlignmentOptions
        {
            Left,
            Center,
            Right,
            Justify
        }

        public const string Space = " ";
        public const char SpaceChar = ' ';
        public const string DefaultText = "The quick brown fox jumps over the lazy dog.";
        public const int MaxBitmapSize = 65535;
        public const int DefaultAntiAliasingLevel = 2;
        public const int MinAntiAliasingLevel = 1;
        public const int MaxAntiAliasingLevel = 6;
        public const TextAlignmentOptions DefaultTextAlignment = TextAlignmentOptions.Left;
        public const int DefaultFontSize = 20;
        public const int MinFontSize = 1;
        public const int MaxFontSize = 400;
        public const double DefaultLetterSpacing = 0;
        public const double MinLetterSpacing = -0.3;
        public const double MaxLetterSpacing = 3;
        public const double DefaultLineSpacing = 0;
        public const double MinLineSpacing = -0.6;
        public const double MaxLineSpacing = 3;
    }
}
