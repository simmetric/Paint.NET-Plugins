using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpacedTextPlugin
{
    using System.Drawing;
    using System.Windows.Forms;
    using PaintDotNet.Effects;

    public class SpacedTextEffectConfigDialog : EffectConfigDialog
    {
        private ComboBox fontName;

        public SpacedTextEffectConfigDialog()
        {
            this.Size = new Size(800, 800);

            fontName = new ComboBox();
            fontName.Items.Add("Comic Sans");
            fontName.Items.Add("Courier New");
            fontName.Items.Add("Impact");
            fontName.Items.Add("Tahoma");
            fontName.Items.Add("Times New Roman");
            fontName.DrawMode = DrawMode.OwnerDrawFixed;
            fontName.DrawItem += FontName_DrawItem;
            fontName.Width = 300;
            this.Controls.Add(fontName);
        }

        private void FontName_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            string itemText = ((ComboBox) sender).Items[e.Index].ToString();

            Font font = new Font(itemText, fontName.ItemHeight - 6);

            e.Graphics.DrawString(itemText, font, Brushes.Black, e.Bounds.X, e.Bounds.Y);
        }
    }
}
