namespace SpacedTextPlugin
{
    using System;
    using System.Drawing;
    using PaintDotNet;
    using PaintDotNet.Collections;
    using PaintDotNet.Drawing;
    using PaintDotNet.Effects;
    using PaintDotNet.PropertySystem;
    using System.Collections.Generic;
    using System.Drawing.Text;
    using System.Linq;
    using PaintDotNet.IndirectUI;
    using PaintDotNet.Rendering;
    using FontStyle = System.Drawing.FontStyle;
    using C = Constants;

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Spaced text")]
    public class SpacedTextEffectsPlugin : PropertyBasedEffect
    {
        private readonly SpacedText helper;
        private readonly List<FontFamily> fontFamilies;

        public SpacedTextEffectsPlugin() : base("Spaced text", null, "Text Formations", EffectFlags.Configurable)
        {
            fontFamilies =
                new InstalledFontCollection().Families.ToList();
            helper = new SpacedText();
        }
        
        protected override PropertyCollection OnCreatePropertyCollection()
        {
            return new PropertyCollection(new List<Property>
            {
                new StringProperty(C.Properties.Text.ToString(), C.DefaultText),
                new Int32Property(C.Properties.FontSize.ToString(), C.DefaultFontSize, C.MinFontSize, C.MaxFontSize),
                new DoubleProperty(C.Properties.LetterSpacing.ToString(), C.DefaultLetterSpacing, C.MinLetterSpacing, C.MaxLetterSpacing),
                new DoubleProperty(C.Properties.LineSpacing.ToString(), C.DefaultLineSpacing, C.MinLineSpacing, C.MaxLineSpacing),
                new Int32Property(C.Properties.AntiAliasLevel.ToString(), C.DefaultAntiAliasingLevel, C.MinAntiAliasingLevel, C.MaxAntiAliasingLevel),
                new StaticListChoiceProperty(C.Properties.FontFamily.ToString(), fontFamilies.ToArray<object>(), fontFamilies.FirstIndexWhere(f => f.Name == "Arial" || f.Name == "Helvetica")),
                new StaticListChoiceProperty(C.Properties.TextAlignment, Enum.GetNames(typeof(C.TextAlignmentOptions)).ToArray<object>(), 0),
                new BooleanProperty(C.Properties.Bold.ToString(), false),
                new BooleanProperty(C.Properties.Italic.ToString(), false),
                new BooleanProperty(C.Properties.Underline.ToString(), false),
                new BooleanProperty(C.Properties.Strikeout.ToString(), false),
            });
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(C.Properties.Text.ToString(), ControlInfoPropertyNames.Multiline, true);
            configUI.SetPropertyControlValue(C.Properties.Bold.ToString(), ControlInfoPropertyNames.DisplayName, "Formatting");
            configUI.SetPropertyControlValue(C.Properties.Bold.ToString(), ControlInfoPropertyNames.Description, C.Properties.Bold.ToString());
            configUI.SetPropertyControlValue(C.Properties.Italic.ToString(), ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(C.Properties.Italic.ToString(), ControlInfoPropertyNames.Description, C.Properties.Italic.ToString());
            configUI.SetPropertyControlValue(C.Properties.Underline.ToString(), ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(C.Properties.Underline.ToString(), ControlInfoPropertyNames.Description, C.Properties.Underline.ToString());
            configUI.SetPropertyControlValue(C.Properties.Strikeout.ToString(), ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(C.Properties.Strikeout.ToString(), ControlInfoPropertyNames.Description, C.Properties.Strikeout.ToString());

            configUI.SetPropertyControlValue(C.Properties.LetterSpacing.ToString(),
                ControlInfoPropertyNames.SliderLargeChange, 0.25);
            configUI.SetPropertyControlValue(C.Properties.LetterSpacing.ToString(),
                ControlInfoPropertyNames.SliderSmallChange, 0.01);
            configUI.SetPropertyControlValue(C.Properties.LetterSpacing.ToString(),
                ControlInfoPropertyNames.UpDownIncrement, 0.01);

            configUI.SetPropertyControlValue(C.Properties.LineSpacing.ToString(),
                ControlInfoPropertyNames.SliderLargeChange, 0.25);
            configUI.SetPropertyControlValue(C.Properties.LineSpacing.ToString(),
                ControlInfoPropertyNames.SliderSmallChange, 0.01);
            configUI.SetPropertyControlValue(C.Properties.LineSpacing.ToString(),
                ControlInfoPropertyNames.UpDownIncrement, 0.01);

            PropertyControlInfo fontControl = configUI.FindControlForPropertyName(C.Properties.FontFamily);
            foreach (FontFamily fontFamily in fontFamilies)
            {
                fontControl.SetValueDisplayName(fontFamily, fontFamily.Name);
            }

            return configUI;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            helper.Text = newToken.GetProperty<StringProperty>(C.Properties.Text.ToString()).Value;
            helper.FontFamily = (FontFamily)newToken.GetProperty<StaticListChoiceProperty>(C.Properties.FontFamily.ToString()).Value;
            helper.FontSize = newToken.GetProperty<Int32Property>(C.Properties.FontSize.ToString()).Value;
            helper.LetterSpacing = newToken.GetProperty<DoubleProperty>(C.Properties.LetterSpacing.ToString()).Value;
            helper.LineSpacing = newToken.GetProperty<DoubleProperty>(C.Properties.LineSpacing.ToString()).Value;
            helper.AntiAliasLevel = newToken.GetProperty<Int32Property>(C.Properties.AntiAliasLevel.ToString()).Value;
            helper.FontStyle = helper.FontFamily.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular : helper.FontFamily.IsStyleAvailable(FontStyle.Bold) ? FontStyle.Bold : FontStyle.Italic;
            helper.TextAlign = (C.TextAlignmentOptions) Enum.Parse(typeof(C.TextAlignmentOptions),
                newToken
                    .GetProperty<StaticListChoiceProperty>(C.Properties.TextAlignment.ToString())
                    .Value.ToString());
            if (newToken.GetProperty<BooleanProperty>(C.Properties.Bold.ToString()).Value && helper.FontFamily.IsStyleAvailable(FontStyle.Bold))
            {
                helper.FontStyle |= FontStyle.Bold;
            }
            if (newToken.GetProperty<BooleanProperty>(C.Properties.Italic.ToString()).Value && helper.FontFamily.IsStyleAvailable(FontStyle.Italic))
            {
                helper.FontStyle |= FontStyle.Italic;
            }
            if (newToken.GetProperty<BooleanProperty>(C.Properties.Underline.ToString()).Value && helper.FontFamily.IsStyleAvailable(FontStyle.Underline))
            {
                helper.FontStyle |= FontStyle.Underline;
            }
            if (newToken.GetProperty<BooleanProperty>(C.Properties.Strikeout.ToString()).Value && helper.FontFamily.IsStyleAvailable(FontStyle.Strikeout))
            {
                helper.FontStyle |= FontStyle.Strikeout;
            }

            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
            helper.IsCancelRequested = IsCancelRequested;
            helper.RenderText(EnvironmentParameters.GetSelection(SrcArgs.Bounds).GetBoundsInt());
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            if (length == 0)
            {
                return;
            }

            //render buffer to destination surface
            for (int i = startIndex; i < startIndex + length; i++)
            {
                if (renderRects[i].IntersectsWith(helper.Bounds))
                {
                    var renderBounds = helper.Bounds;
                    renderBounds.Intersect(renderRects[i]);

                    //since TextOut does not support transparent text, we will use the resulting bitmap as a transparency map
                    CopyRectangle(renderBounds, helper.BufferSurface, DstArgs.Surface);

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

        private void CopyRectangle(Rectangle area, Surface buffer, Surface dest)
        {
            for (int y = area.Top; y < area.Bottom; y++)
            {
                for (int x = area.Left; x < area.Right; x++)
                {
                    //use the buffer as an alpha map
                    ColorBgra color = EnvironmentParameters.PrimaryColor;
                    ColorBgra opacitySource = buffer[x - helper.Bounds.Left, y - helper.Bounds.Top];
                    color.A = opacitySource.R;
                    dest[x, y] = color;
                }
            }
        }
    }
}
