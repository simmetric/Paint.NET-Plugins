namespace Shared.Data
{
    using PaintDotNet.Effects;
    using PaintDotNet.PropertySystem;
    using System;
    using System.Drawing;
    using C = Shared.Data.Constants;

    public class Settings
    {
        public string Text { get; set; }
        public FontFamily FontFamily { get; set; }
        public float FontSize { get; set; }
        public double LetterSpacing { get; set; }
        public double LineSpacing { get; set; }
        public int AntiAliasLevel { get; set; }
        public FontStyle FontStyle { get; set; }

        public Font GetFont()
        {
            return new Font(FontFamily, FontSize, FontStyle, GraphicsUnit.Pixel);
        }

        public Font GetAntiAliasSizeFont()
        {
            return new Font(FontFamily, FontSize * AntiAliasLevel, FontStyle, GraphicsUnit.Pixel);
        }

        public Settings(PropertyBasedEffectConfigToken newToken)
        {
            Text = newToken.GetProperty<StringProperty>(C.Properties.Text.ToString()).Value;
            FontFamily = (FontFamily)newToken.GetProperty<StaticListChoiceProperty>(C.Properties.FontFamily.ToString()).Value;
            LetterSpacing = newToken.GetProperty<DoubleProperty>(C.Properties.LetterSpacing.ToString()).Value;
            LineSpacing = newToken.GetProperty<DoubleProperty>(C.Properties.LineSpacing.ToString()).Value;
            AntiAliasLevel = newToken.GetProperty<Int32Property>(C.Properties.AntiAliasLevel.ToString()).Value;
            FontStyle = FontFamily.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular : FontFamily.IsStyleAvailable(FontStyle.Bold) ? FontStyle.Bold : FontStyle.Italic;

            if (newToken.GetProperty<BooleanProperty>(C.Properties.Bold.ToString()).Value && FontFamily.IsStyleAvailable(FontStyle.Bold))
            {
                FontStyle |= FontStyle.Bold;
            }
            if (newToken.GetProperty<BooleanProperty>(C.Properties.Italic.ToString()).Value && FontFamily.IsStyleAvailable(FontStyle.Italic))
            {
                FontStyle |= FontStyle.Italic;
            }
            if (newToken.GetProperty<BooleanProperty>(C.Properties.Underline.ToString()).Value && FontFamily.IsStyleAvailable(FontStyle.Underline))
            {
                FontStyle |= FontStyle.Underline;
            }
            if (newToken.GetProperty<BooleanProperty>(C.Properties.Strikeout.ToString()).Value && FontFamily.IsStyleAvailable(FontStyle.Strikeout))
            {
                FontStyle |= FontStyle.Strikeout;
            }
        }
    }
}
