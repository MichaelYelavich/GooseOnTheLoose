using System;
using System.Windows.Forms;

namespace SecondHonk
{
    // Never shown. Common dialogs need a live top-level owner, independent of optional tools.
    public sealed class ToolDialogOwner : Form
    {
        public ToolDialogOwner()
        {
            Text = "Goose on the Loose tools";
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.CenterScreen;
            var handle = Handle;
        }

        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var p = base.CreateParams;
                p.ExStyle |= 0x80 | 0x08000000;
                return p;
            }
        }
    }
}
