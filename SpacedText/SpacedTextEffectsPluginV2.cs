using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpacedTextPlugin
{
    using System.Drawing;
    using PaintDotNet;
    using PaintDotNet.Effects;
    public class SpacedTextEffectsPluginV2 : Effect
    {
        public SpacedTextEffectsPluginV2() : base("Spaced Text as effect", null, "Text Formations",
            EffectFlags.Configurable)
        {
            
        }

        public override EffectConfigDialog CreateConfigDialog()
        {
            return new SpacedTextEffectConfigDialog();
        }

        public override void Render(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs, Rectangle[] rois, int startIndex,
            int length)
        {
        }
    }
}
