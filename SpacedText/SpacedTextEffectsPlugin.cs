namespace SpacedTextPlugin
{
    using System;
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
    using PaintDotNet.IndirectUI;
    using PaintDotNet.Rendering;
    using Bitmap = System.Drawing.Bitmap;
    using FontStyle = System.Drawing.FontStyle;

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Spaced text")]
    public class SpacedTextEffectsPlugin : PropertyBasedEffect
    {
        private string Text;
        private string FontFamily;
        private int FontSize;
        private double LetterSpacing;
        private double LineSpacing;
        private int AntiAliasLevel;
        private FontStyle FontStyle;
        private Constants.TextAlignmentOptions TextAlign;

        private readonly string[] FontFamilies;

        private Surface surf;
        private Rectangle bounds;

        private ColorMap[] colorMap;
        private ImageAttributes imgAttr;

        public SpacedTextEffectsPlugin() : base("Spaced text", null, "Text Formations", EffectFlags.Configurable)
        {
            FontFamilies =
                UIUtil.GetGdiFontNames()
                    .Where(f => f.Item2 == UIUtil.GdiFontType.TrueType)
                    .Select(f => f.Item1).OrderBy(f => f)
                    .ToArray();

            AntiAliasLevel = 2;

            colorMap = new ColorMap[]
            {
                new ColorMap
                {
                    OldColor = Color.Black,
                    NewColor = Color.Transparent
                }
            };
            imgAttr = new ImageAttributes();
            imgAttr.SetRemapTable(colorMap);
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

                    //clear the remainder
                    DstArgs.Surface.Clear(new RectInt32(
                        renderRects[i].X,
                        renderBounds.Bottom,
                        renderRects[i].Width,
                        renderRects[i].Bottom - renderBounds.Bottom
                    ), ColorBgra.Transparent);

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

            //render text on larger bitmap so it can be anti-aliased while scaling down
            bm = new Bitmap(bounds.Size.Width*AntiAliasLevel, bounds.Size.Height*AntiAliasLevel);
            gr = Graphics.FromImage(bm);
            gr.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            gr.SmoothingMode = SmoothingMode.HighQuality;
            gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gr.CompositingQuality = CompositingQuality.HighQuality;
            gr.CompositingMode = CompositingMode.SourceOver;
            gr.TextContrast = 1;
            gr.Clear(Color.Black);

            //split in lines
            float letterSpacing = 0;
            if (TextAlign != Constants.TextAlignmentOptions.Justify)
            {
                letterSpacing = (float)LetterSpacing;
            }
            var words = Text.Split(' ');
            List<string> lines = new List<string>();
            string currentLine = string.Empty;
            foreach (string word in words)
            {
                //measure currentline + word
                //else add word to currentline
                if (PInvoked.MeasureString(gr, currentLine + " " + word, font, letterSpacing).Width > bm.Width - FontSize)
                {
                    //if outside bounds, then add line
                    lines.Add(currentLine.Trim());
                    currentLine = word + " ";
                }
                else
                {
                    currentLine += word + " ";
                }
            }
            //add currentline
            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            //draw lines
            //justify if necessary
            int lineStart = 0;
            foreach (string line in lines)
            {
                int left = FontSize / 2;
                Size textBounds;
                if (TextAlign != Constants.TextAlignmentOptions.Justify)
                {
                    //measure text
                    textBounds = PInvoked.MeasureString(gr, line, font, letterSpacing);
                    if (TextAlign != Constants.TextAlignmentOptions.Left)
                    {
                        if (TextAlign == Constants.TextAlignmentOptions.Center)
                        {
                            left = bm.Width / 2 - textBounds.Width / 2;

                        }
                        else if (TextAlign == Constants.TextAlignmentOptions.Right)
                        {
                            left = bm.Width - textBounds.Width;
                        }
                    }
                }
                else
                {
                    textBounds = PInvoked.MeasureString(gr, line, font, letterSpacing);
                    while (textBounds.Width <= bm.Width - FontSize)
                    {
                        letterSpacing += 0.01f;
                        textBounds = PInvoked.MeasureString(gr, line, font, letterSpacing);
                    }
                }

                //create new bitmap for line
                Bitmap lineBm = new Bitmap(textBounds.Width * AntiAliasLevel, textBounds.Height * AntiAliasLevel);
                Graphics lineGr = Graphics.FromImage(lineBm);
                //draw text
                PInvoked.TextOut(lineGr, line, 0, 0, font, letterSpacing);
                //draw lineBm to bm leaving out black
                gr.DrawImage(lineBm, new Rectangle(new Point(left, lineStart), textBounds), 0, 0, textBounds.Width, textBounds.Height, GraphicsUnit.Pixel, imgAttr);
                lineGr.Dispose();
                lineBm.Dispose();

                lineStart += font.Height + (int)Math.Round((double)font.Height * LineSpacing);
            }

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
                new StringProperty(Constants.Properties.Text.ToString(),
                    "The quick brown fox jumps over the lazy dog."),
                new Int32Property(Constants.Properties.FontSize.ToString(), 20, 1, 500),
                new DoubleProperty(Constants.Properties.LetterSpacing.ToString(), 0, -0.3, 3),
                new DoubleProperty(Constants.Properties.LineSpacing.ToString(), 0, -0.6, 3),
                new Int32Property(Constants.Properties.AntiAliasLevel.ToString(), 2, 1, 8),
                new StaticListChoiceProperty(Constants.Properties.FontFamily.ToString(), FontFamilies, FontFamilies.FirstIndexWhere(f => f == "Arial")),
                new StaticListChoiceProperty(Constants.Properties.TextAlignment, System.Enum.GetNames(typeof(Constants.TextAlignmentOptions)).Except(Constants.TextAlignmentOptions.Justify.ToString()).ToArray(), 0),
                new BooleanProperty(Constants.Properties.Bold.ToString(), false),
                new BooleanProperty(Constants.Properties.Italic.ToString(), false),
                new BooleanProperty(Constants.Properties.Underline.ToString(), false),
                new BooleanProperty(Constants.Properties.Strikeout.ToString(), false),
            });
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            var configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(Constants.Properties.LetterSpacing.ToString(),
                ControlInfoPropertyNames.SliderLargeChange, 0.25);
            configUI.SetPropertyControlValue(Constants.Properties.LetterSpacing.ToString(),
                ControlInfoPropertyNames.SliderSmallChange, 0.01);
            configUI.SetPropertyControlValue(Constants.Properties.LetterSpacing.ToString(),
                ControlInfoPropertyNames.UpDownIncrement, 0.01);

            configUI.SetPropertyControlValue(Constants.Properties.LineSpacing.ToString(),
                ControlInfoPropertyNames.SliderLargeChange, 0.25);
            configUI.SetPropertyControlValue(Constants.Properties.LineSpacing.ToString(),
                ControlInfoPropertyNames.SliderSmallChange, 0.01);
            configUI.SetPropertyControlValue(Constants.Properties.LineSpacing.ToString(),
                ControlInfoPropertyNames.UpDownIncrement, 0.01);

            return configUI;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            this.Text = newToken.GetProperty<StringProperty>(Constants.Properties.Text.ToString()).Value;
            this.FontFamily = newToken.GetProperty<StaticListChoiceProperty>(Constants.Properties.FontFamily.ToString()).Value.ToString();
            this.FontSize = newToken.GetProperty<Int32Property>(Constants.Properties.FontSize.ToString()).Value;
            this.LetterSpacing = newToken.GetProperty<DoubleProperty>(Constants.Properties.LetterSpacing.ToString()).Value;
            this.LineSpacing = newToken.GetProperty<DoubleProperty>(Constants.Properties.LineSpacing.ToString()).Value;
            this.AntiAliasLevel = newToken.GetProperty<Int32Property>(Constants.Properties.AntiAliasLevel.ToString()).Value;
            var fontFamily = new FontFamily(this.FontFamily);
            this.FontStyle = fontFamily.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular : FontStyle.Bold;
            this.TextAlign = (Constants.TextAlignmentOptions) Enum.Parse(typeof(Constants.TextAlignmentOptions),
                newToken
                    .GetProperty<StaticListChoiceProperty>(Constants.Properties.TextAlignment.ToString())
                    .Value.ToString());
            if (newToken.GetProperty<BooleanProperty>(Constants.Properties.Bold.ToString()).Value && fontFamily.IsStyleAvailable(FontStyle.Bold))
            {
                this.FontStyle |= FontStyle.Bold;
            }
            if (newToken.GetProperty<BooleanProperty>(Constants.Properties.Italic.ToString()).Value && fontFamily.IsStyleAvailable(FontStyle.Italic))
            {
                this.FontStyle |= FontStyle.Italic;
            }
            if (newToken.GetProperty<BooleanProperty>(Constants.Properties.Underline.ToString()).Value && fontFamily.IsStyleAvailable(FontStyle.Underline))
            {
                this.FontStyle |= FontStyle.Underline;
            }
            if (newToken.GetProperty<BooleanProperty>(Constants.Properties.Strikeout.ToString()).Value && fontFamily.IsStyleAvailable(FontStyle.Strikeout))
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
                    ColorBgra opacitySource = buffer[x - bounds.Left, y - bounds.Top];
                    color.A = (byte) (opacitySource.R);
                    dest[x, y] = color;
                }
            }
        }
    }
}
