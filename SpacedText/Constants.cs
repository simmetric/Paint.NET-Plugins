using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpacedTextPlugin
{
    public class Constants
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
