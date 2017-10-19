namespace SpacedTextPlugin.Data
{
    using System;
    using PaintDotNet.Effects;
    using PaintDotNet.PropertySystem;
    using Shared.Data;

    internal class Settings:Shared.Data.Settings
    {
        public Constants.TextAlignmentOptions TextAlign { get; set; }

        public Settings(PropertyBasedEffectConfigToken newToken) : base(newToken)
        {
            FontSize = newToken.GetProperty<Int32Property>(Constants.Properties.FontSize.ToString()).Value;
            TextAlign = (Constants.TextAlignmentOptions)Enum.Parse(typeof(Constants.TextAlignmentOptions),
                newToken
                    .GetProperty<StaticListChoiceProperty>(Constants.Properties.TextAlignment.ToString())
                    .Value.ToString());
        }
    }
}
