﻿namespace SpacedTextPlugin
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
    using System.Reflection;
    using PaintDotNet.IndirectUI;
    using PaintDotNet.Rendering;
    using SpacedTextPlugin.Data;
    using FontStyle = System.Drawing.FontStyle;
    using C = Shared.Data.Constants;

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Spaced text")]
    public class SpacedTextEffectsPlugin : PropertyBasedEffect
    {
        private readonly SpacedText helper;
        private readonly List<FontFamily> fontFamilies;

        public SpacedTextEffectsPlugin() : base("Spaced text", StaticIcon, "Text Formations", EffectFlags.Configurable)
        {
            fontFamilies =
                new InstalledFontCollection().Families.ToList();
            helper = new SpacedText();
        }

        public static string StaticName => "Spaced text";

        public static Image StaticIcon => System.Drawing.Image.FromStream(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("SpacedTextPlugin.SpacedTextIcon16.png"));

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
#pragma warning disable S4142 // Duplicate values should not be passed as arguments
            configUI.SetPropertyControlValue(C.Properties.Bold.ToString(), ControlInfoPropertyNames.Description, C.Properties.Bold.ToString());
            configUI.SetPropertyControlValue(C.Properties.Italic.ToString(), ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(C.Properties.Italic.ToString(), ControlInfoPropertyNames.Description, C.Properties.Italic.ToString());
            configUI.SetPropertyControlValue(C.Properties.Underline.ToString(), ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(C.Properties.Underline.ToString(), ControlInfoPropertyNames.Description, C.Properties.Underline.ToString());
            configUI.SetPropertyControlValue(C.Properties.Strikeout.ToString(), ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue(C.Properties.Strikeout.ToString(), ControlInfoPropertyNames.Description, C.Properties.Strikeout.ToString());
#pragma warning restore S4142 // Duplicate values should not be passed as arguments

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

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
            helper.IsCancelRequested = IsCancelRequested;
            helper.RenderText(EnvironmentParameters.GetSelection(SrcArgs.Bounds), new Settings(newToken));
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

    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author => ((AssemblyCopyrightAttribute)GetType().Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0]).Copyright;

        public string Copyright => ((AssemblyDescriptionAttribute)GetType().Assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0]).Description;

        public string DisplayName => ((AssemblyProductAttribute) GetType().Assembly
            .GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0]).Product;

        public Version Version => GetType().Assembly.GetName().Version;

        public Uri WebsiteUri => new Uri("https://github.com/simmetric/");
    }
}