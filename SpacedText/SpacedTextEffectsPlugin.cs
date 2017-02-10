namespace SpacedTextPlugin
{
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Text;
    using PaintDotNet;
    using PaintDotNet.Collections;
    using PaintDotNet.Drawing;
    using PaintDotNet.Effects;
    using PaintDotNet.PropertySystem;
    using PaintDotNet.SystemLayer;
    using System.Collections.Generic;
    using System.Linq;
    using PaintDotNet.IndirectUI;
    using Bitmap = System.Drawing.Bitmap;
    using FontStyle = System.Drawing.FontStyle;

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Spaced text")]
    public class Test : PropertyBasedEffect
    {
        private string Text;
        private string FontFamily;
        private int FontSize;
        private double LetterSpacing;
        private FontStyle FontStyle;

        private readonly string[] FontFamilies;

        private StringFormat frmt;

        public Test() : base("Spaced text", null, "Text", EffectFlags.Configurable)
        {
            FontFamilies =
                UIUtil.GetGdiFontNames()
                    .Where(f => f.Item2 == UIUtil.GdiFontType.TrueType)
                    .Select(f => f.Item1).OrderBy(f => f)
                    .ToArray();

            frmt = (StringFormat)StringFormat.GenericDefault.Clone();
            frmt.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            if (length == 0)
            {
                return;
            }

            var font = new Font(FontFamily, FontSize * 2, FontStyle, GraphicsUnit.Point);

            //render text on 4x bitmap so it can be anti-aliased while scaling down
            var bm = new Bitmap(base.SrcArgs.Width * 2, font.Height);
            var gr = Graphics.FromImage(bm);
            gr.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            gr.SmoothingMode = SmoothingMode.HighQuality;
            gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gr.CompositingQuality = CompositingQuality.HighQuality;
            gr.CompositingMode = CompositingMode.SourceOver;
            gr.TextContrast = 1;
            gr.Clear(Color.Transparent);

            //draw text
            PInvoked.TextOut(gr, Text, 0, 0, font, (float)LetterSpacing);

            //scale bitmap down onto result-size bitmap and apply anti-aliasing
            var resultBm = new Bitmap(SrcArgs.Width, SrcArgs.Height);
            var resultGr = Graphics.FromImage(resultBm);
            resultGr.SmoothingMode = SmoothingMode.HighQuality;
            resultGr.InterpolationMode = InterpolationMode.HighQualityBicubic;
            resultGr.CompositingQuality = CompositingQuality.HighQuality;
            resultGr.CompositingMode = CompositingMode.SourceOver;
            resultGr.DrawImage(bm, new Rectangle(0, 0, resultBm.Width, font.Height/2));
            var surf = Surface.CopyFromBitmap(resultBm);

            //render bitmap to destination surface
            for (int i = startIndex; i < startIndex + length; i++)
            {
                if (renderRects[i].Top < font.Height / 2)
                {
                    //since TextOut does not support transparent text, we will use the resulting bitmap as a transparency map
                    CopyRectangle(renderRects[i], surf, base.DstArgs.Surface);
                }
                else
                {
                    DstArgs.Surface.Clear(renderRects[i].ToRectInt32(), ColorBgra.Transparent);
                }
            }

            //cleanup
            gr.Dispose();
            bm.Dispose();
            resultGr.Dispose();
            resultBm.Dispose();
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            return new PropertyCollection(new List<Property>
            {
                new StringProperty("Text",
                    "The quick brown fox jumps over the lazy dog."),
                new Int32Property("FontSize", 20, 1, 500),
                new DoubleProperty("LetterSpacing", 0, -0.5, 5),

                new StaticListChoiceProperty("FontFamily", FontFamilies, FontFamilies.FirstIndexWhere(f => f == "Arial")),
                new BooleanProperty("Bold", false),
                new BooleanProperty("Italic", false),
                new BooleanProperty("Underline", false),
                new BooleanProperty("Strikeout", false),
            });
        }
        
        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            this.Text = newToken.GetProperty<StringProperty>("Text").Value;
            this.FontFamily = newToken.GetProperty<StaticListChoiceProperty>("FontFamily").Value.ToString();
            this.FontSize = newToken.GetProperty<Int32Property>("FontSize").Value;
            this.LetterSpacing = newToken.GetProperty<DoubleProperty>("LetterSpacing").Value;

            var fontFamily = new FontFamily(this.FontFamily);
            this.FontStyle = fontFamily.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular : FontStyle.Bold;
            if (newToken.GetProperty<BooleanProperty>("Bold").Value && fontFamily.IsStyleAvailable(FontStyle.Bold))
            {
                this.FontStyle |= FontStyle.Bold;
            }
            if (newToken.GetProperty<BooleanProperty>("Italic").Value && fontFamily.IsStyleAvailable(FontStyle.Italic))
            {
                this.FontStyle |= FontStyle.Italic;
            }
            if (newToken.GetProperty<BooleanProperty>("Underline").Value && fontFamily.IsStyleAvailable(FontStyle.Underline))
            {
                this.FontStyle |= FontStyle.Underline;
            }
            if (newToken.GetProperty<BooleanProperty>("Strikeout").Value && fontFamily.IsStyleAvailable(FontStyle.Strikeout))
            {
                this.FontStyle |= FontStyle.Strikeout;
            }

            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        private void CopyRectangle(Rectangle area, Surface buffer, Surface dest)
        {
            for (int y = area.Top; y < area.Bottom; y++)
            {
                for (int x = area.Left; x < area.Right; x++)
                {
                    //use the buffer as an alpha map
                    ColorBgra color = base.EnvironmentParameters.PrimaryColor;
                    ColorBgra opacitySource = buffer[x, y];
                    color.A = (byte)(opacitySource.R);
                    dest[x, y] = color;
                }
            }
        }
    }
}
