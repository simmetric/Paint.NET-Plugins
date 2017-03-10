using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpacedTextPlugin
{
    using System.Runtime.Remoting.Messaging;

    public class Constants
    {
        public static class PropertyNames
        {
            public const string Text = "Text";
            public const string FontSize = "FontSize";
            public const string LetterSpacing = "LetterSpacing";
            public const string LineSpacing = "LineSpacing";
            public const string AntiAliasLevel = "AntiAliasLevel";
            public const string FontFamily = "FontFamily";
            public const string Bold = "Bold";
            public const string Italic = "Italic";
            public const string Underline = "Underline";
            public const string Strikeout = "Strikeout";
            public const string TextAlignment = "TextAlignment";
            public const string JustifyText = "JustifyText";
            public const string JustifyExceptLastLine = "JustifyExceptLastLine";
        }

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
            TextAlignment,
            JustifyText,
            JustifyExceptLastLine
        }

        public enum TextAlignmentOptions
        {
            Left,
            Center,
            Right,
            Justify
        }
    }
}
