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
    using System.Drawing.Imaging;
    using System.Linq;
    using Bitmap = System.Drawing.Bitmap;
    using FontStyle = System.Drawing.FontStyle;

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Spaced text")]
    public class SpacedTextEffectsPlugin : PropertyBasedEffect
    {
        private string Text;
        private string FontFamily;
        private int FontSize;
        private double LetterSpacing;
        private int AntiAliasLevel;
        private FontStyle FontStyle;

        private readonly string[] FontFamilies;

        private StringFormat frmt;

        private Surface surf;
        private Rectangle bounds;

        public SpacedTextEffectsPlugin() : base("Spaced text", null, "Text", EffectFlags.Configurable)
        {
            FontFamilies =
                UIUtil.GetGdiFontNames()
                    .Where(f => f.Item2 == UIUtil.GdiFontType.TrueType)
                    .Select(f => f.Item1).OrderBy(f => f)
                    .ToArray();

            frmt = (StringFormat)StringFormat.GenericDefault.Clone();
            frmt.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

            AntiAliasLevel = 1;
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            if (length == 0)
            {
                return;
            }

            //render bitmap to destination surface
            for (int i = startIndex; i < startIndex + length; i++)
            {
                if (renderRects[i].IntersectsWith(bounds))
                {
                    var renderBounds = bounds;
                    renderBounds.Intersect(renderRects[i]);

                    //since TextOut does not support transparent text, we will use the resulting bitmap as a transparency map
                    CopyRectangle(renderBounds, surf, base.DstArgs.Surface);
                }
                else
                {
                    DstArgs.Surface.Clear(renderRects[i].ToRectInt32(), ColorBgra.Transparent);
                }
            }
        }

        private void RenderText()
        {
            Bitmap bm;
            Graphics gr;
            Bitmap resultBm;
            Graphics resultGr;
            var font = new Font(FontFamily, FontSize*AntiAliasLevel, FontStyle, GraphicsUnit.Pixel);

            bounds = EnvironmentParameters.GetSelection(SrcArgs.Bounds).GetBoundsInt();
            if (bounds.Equals(SrcArgs.Bounds))
            {
                //calculate bounds based on font size;
                bounds = new Rectangle(0, 0, SrcArgs.Width, font.Height);
            }

            //render text on larger bitmap so it can be anti-aliased while scaling down
            bm = new Bitmap(bounds.Size.Width*AntiAliasLevel, bounds.Size.Height*AntiAliasLevel);
            gr = Graphics.FromImage(bm);
            gr.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            gr.SmoothingMode = SmoothingMode.HighQuality;
            gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gr.CompositingQuality = CompositingQuality.HighQuality;
            gr.CompositingMode = CompositingMode.SourceOver;
            gr.TextContrast = 1;
            gr.Clear(Color.Transparent);

            //measure text
            var size = PInvoked.MeasureString(gr, Text, font, (float) LetterSpacing);

            //draw text
            PInvoked.TextOut(gr, Text, bm.Width/2 - size.Width/2, 0, font, (float) LetterSpacing);

            //scale bitmap down onto result-size bitmap and apply anti-aliasing
            resultBm = new Bitmap(bounds.Width, bounds.Height);
            resultGr = Graphics.FromImage(resultBm);
            resultGr.SmoothingMode = SmoothingMode.HighQuality;
            resultGr.InterpolationMode = InterpolationMode.HighQualityBicubic;
            resultGr.CompositingQuality = CompositingQuality.HighQuality;
            resultGr.CompositingMode = CompositingMode.SourceOver;
            resultGr.DrawImage(bm, 0f, 0f, bounds.Width, bounds.Height);
            surf = Surface.CopyFromBitmap(resultBm);

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
                new DoubleProperty("LetterSpacing", 0, -0.3, 3),
                new Int32Property("AntiAliasLevel", 2, 1, 8),
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
            this.AntiAliasLevel = newToken.GetProperty<Int32Property>("AntiAliasLevel").Value;
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

            RenderText();
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
                    color.A = (byte) (opacitySource.R);
                    dest[x, y] = color;
                }
            }
        }
    }
}
