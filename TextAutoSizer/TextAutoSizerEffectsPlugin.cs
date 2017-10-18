namespace TextAutoSizerPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Text;
    using System.Linq;
    using PaintDotNet;
    using PaintDotNet.Collections;
    using PaintDotNet.Effects;
    using PaintDotNet.IndirectUI;
    using PaintDotNet.PropertySystem;
    using TextAutoSizerPlugin.Data;
    using C = Shared.Data.Constants;

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Text Autosizer")]
    public class TextAutoSizerEffectsPlugin : PropertyBasedEffect
    {
        private readonly List<FontFamily> fontFamilies;

        public TextAutoSizerEffectsPlugin() : base("Text Autosizer", null, "Text Formations", EffectFlags.Configurable)
        {
            fontFamilies =
                new InstalledFontCollection().Families.ToList();
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

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
    }
}
