﻿namespace SpacedTextPlugin
{
    using System;
    using System.Drawing;
    using PaintDotNet;
    using PaintDotNet.Collections;
    using PaintDotNet.Drawing;
    using PaintDotNet.Effects;
    using PaintDotNet.PropertySystem;
    using PaintDotNet.SystemLayer;
    using System.Collections.Generic;
    using System.Linq;
    using PaintDotNet.IndirectUI;
    using PaintDotNet.Rendering;
    using FontStyle = System.Drawing.FontStyle;

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Spaced text")]
    public class SpacedTextEffectsPlugin : PropertyBasedEffect
    {
        private readonly SpacedText helper;
        private readonly List<string> fontFamilies;

        public SpacedTextEffectsPlugin() : base("Spaced text", null, "Text Formations", EffectFlags.Configurable)
        {
            fontFamilies =
                UIUtil.GetGdiFontNames()
                    .Where(f => f.Item2 == UIUtil.GdiFontType.TrueType)
                    .Select(f => f.Item1).OrderBy(f => f)
                    .ToList();
            helper = new SpacedText();
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
                new StaticListChoiceProperty(Constants.Properties.FontFamily.ToString(), fontFamilies.ToArray<object>(), fontFamilies.FirstIndexWhere(f => f == "Arial")),
                new StaticListChoiceProperty(Constants.Properties.TextAlignment, Enum.GetNames(typeof(Constants.TextAlignmentOptions)).ToArray<object>(), 0),
                new BooleanProperty(Constants.Properties.Bold.ToString(), false),
                new BooleanProperty(Constants.Properties.Italic.ToString(), false),
                new BooleanProperty(Constants.Properties.Underline.ToString(), false),
                new BooleanProperty(Constants.Properties.Strikeout.ToString(), false),
            });
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(Constants.Properties.Text.ToString(), ControlInfoPropertyNames.Multiline, true);
            configUI.SetPropertyControlValue(Constants.Properties.Bold.ToString(), ControlInfoPropertyNames.DisplayName, "Formatting");
            configUI.SetPropertyControlValue(Constants.Properties.Bold.ToString(), ControlInfoPropertyNames.Description, Constants.Properties.Bold.ToString());
            configUI.SetPropertyControlValue(Constants.Properties.Italic.ToString(), ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(Constants.Properties.Italic.ToString(), ControlInfoPropertyNames.Description, Constants.Properties.Italic.ToString());
            configUI.SetPropertyControlValue(Constants.Properties.Underline.ToString(), ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(Constants.Properties.Underline.ToString(), ControlInfoPropertyNames.Description, Constants.Properties.Underline.ToString());
            configUI.SetPropertyControlValue(Constants.Properties.Strikeout.ToString(), ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(Constants.Properties.Strikeout.ToString(), ControlInfoPropertyNames.Description, Constants.Properties.Strikeout.ToString());

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
            helper.Text = newToken.GetProperty<StringProperty>(Constants.Properties.Text.ToString()).Value;
            helper.FontFamily = newToken.GetProperty<StaticListChoiceProperty>(Constants.Properties.FontFamily.ToString()).Value.ToString();
            helper.FontSize = newToken.GetProperty<Int32Property>(Constants.Properties.FontSize.ToString()).Value;
            helper.LetterSpacing = newToken.GetProperty<DoubleProperty>(Constants.Properties.LetterSpacing.ToString()).Value;
            helper.LineSpacing = newToken.GetProperty<DoubleProperty>(Constants.Properties.LineSpacing.ToString()).Value;
            helper.AntiAliasLevel = newToken.GetProperty<Int32Property>(Constants.Properties.AntiAliasLevel.ToString()).Value;
            var fontFamily = new FontFamily(helper.FontFamily);
            helper.FontStyle = fontFamily.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular : FontStyle.Bold;
            helper.TextAlign = (Constants.TextAlignmentOptions) Enum.Parse(typeof(Constants.TextAlignmentOptions),
                newToken
                    .GetProperty<StaticListChoiceProperty>(Constants.Properties.TextAlignment.ToString())
                    .Value.ToString());
            if (newToken.GetProperty<BooleanProperty>(Constants.Properties.Bold.ToString()).Value && fontFamily.IsStyleAvailable(FontStyle.Bold))
            {
                helper.FontStyle |= FontStyle.Bold;
            }
            if (newToken.GetProperty<BooleanProperty>(Constants.Properties.Italic.ToString()).Value && fontFamily.IsStyleAvailable(FontStyle.Italic))
            {
                helper.FontStyle |= FontStyle.Italic;
            }
            if (newToken.GetProperty<BooleanProperty>(Constants.Properties.Underline.ToString()).Value && fontFamily.IsStyleAvailable(FontStyle.Underline))
            {
                helper.FontStyle |= FontStyle.Underline;
            }
            if (newToken.GetProperty<BooleanProperty>(Constants.Properties.Strikeout.ToString()).Value && fontFamily.IsStyleAvailable(FontStyle.Strikeout))
            {
                helper.FontStyle |= FontStyle.Strikeout;
            }

            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

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
                    CopyRectangle(renderBounds, helper.BufferSurface, base.DstArgs.Surface);

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
                    ColorBgra color = base.EnvironmentParameters.PrimaryColor;
                    ColorBgra opacitySource = buffer[x - helper.Bounds.Left, y - helper.Bounds.Top];
                    color.A = opacitySource.R;
                    dest[x, y] = color;
                }
            }
        }
    }
}
