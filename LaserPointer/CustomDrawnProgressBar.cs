using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace LaserPointer
{
    public class CustomDrawnProgressBar : ProgressBar
    {
        const int PP_BAR = 1;
        const int PP_FILL = 5;
        const int PBFS_NORMAL = 1;

        public CustomDrawnProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var vs1 = new VisualStyleRenderer("PROGRESS", PP_BAR, 0);
            vs1.DrawBackground(e.Graphics, ClientRectangle);
            var vs2 = new VisualStyleRenderer("PROGRESS", PP_FILL, PBFS_NORMAL);
            vs2.DrawBackground(e.Graphics, new System.Drawing.Rectangle(
                0, 0,
                (int)(Width * ((Value - Minimum) / (double)(Maximum - Minimum))), Height
            ));
        }
    }
}
