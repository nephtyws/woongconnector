using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace WoongConnector
{
    public sealed partial class MapleForm : Form
    {
        public ContextMenu TrayMenu;
        public MapleMode Mode;
        public enum MapleMode
        {
            MSEA,
            EMS,
            GMS,
            KMS
        }
        public MapleForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Bisque;
            TransparencyKey = Color.Bisque;
            Mode = Program.Mode;
            TrayMenu = new ContextMenu();

            Program.Start();

            new NotifyIcon
            {
                Icon = new Icon(Icon, 40, 40),
                ContextMenu = TrayMenu,
                Visible = true
            };

            if (Mode != MapleMode.GMS)
                ShowInTaskbar = false;

            new Thread(Program.Start).Start();
        }
    }
}
