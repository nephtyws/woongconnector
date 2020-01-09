using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using static WoongConnector.MapleForm;

namespace WoongConnector
{
    static class Program
    {
        public static MapleForm Form;
        public static string Host = "";
        public static ushort[] Ports = { 8484, 8585, 8586, 8587, 8700, 9700, 9900, 12000 };

        public static System.Timers.Timer Timer = new System.Timers.Timer();
        public static MapleMode Mode = MapleMode.KMS;
        public static bool NeedResolveDNS = false;
        public static bool UseGUI = false;
        public static Process Maple;
        public static Thread ProcessCheckThread;

        [STAThread]
        private static void Main()
        {
            if (isRunning())
            {
                Environment.Exit(0);
                return;
            }

            if (NeedResolveDNS)
                Host = getIP();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Form = new MapleForm();

            if (UseGUI || Mode == MapleMode.GMS)
                Application.Run(Form);

            else
                Application.Run();
        }
        public static void Start()
        {
            try
            {
                SetNexonDNS();
                OccupyNexonIP();
                StartTunnel();
            }

            finally
            {
                MapleLauncher();

                ProcessCheckThread = new Thread(ProcessTimer);
                ProcessCheckThread.Start();
            }
        }

        public static void ProcessTimer()
        {
            Timer.Interval = 5000.0;
            Timer.Elapsed += ProcessCheckTick;
            Timer.Start();
        }
        private static void SetNexonDNS()
        {
            new Process
            {
                StartInfo =
                {
                    FileName = "netsh.exe",
                    Arguments = "interface ip set dns \"Loopback Pseudo-Interface 1\" dhcp",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            }
            .Start();
        }
        public static void ProcessCheckTick(object sender, EventArgs e)
        {
            if (Process.GetProcessesByName(Maple.ProcessName).Length >= 1)
                return;

            try
            {
                ReleaseNexonIP();
            }

            finally
            {
                Application.Exit();
            }
        }
        private static void MapleLauncher()
        {
            var currentDirectory = Directory.GetCurrentDirectory();

            if (!File.Exists($"{currentDirectory}/Maplestory.exe"))
            {
                MessageBox.Show("Failed to find Maplestory.exe. Please check your directory!", "Error");
                Application.Exit();
            }

            Maple = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine(currentDirectory, "Maplestory.exe"), Arguments = "GameLaunching"
                }
            };

            Maple.Start();
        }
        private static void OccupyNexonIP()
        {
            Process process = new Process();

            for (var index = 0; index <= 255; ++index)
            {
                process.StartInfo.FileName = "netsh.exe";
                process.StartInfo.Arguments = $"int ip add addr 1 address=175.207.0.{index} st=ac";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
            }
        }

        private static void ReleaseNexonIP()
        {
            Process process = new Process();

            for (var index = 0; index <= 255; ++index)
            {
                process.StartInfo.FileName = "netsh.exe";
                process.StartInfo.Arguments = $"int ip delete addr 1 175.207.0.{index} st=ac";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
            }
        }
        public static void StartTunnel()
        {
            try
            {
                foreach (var port in Ports)
                {
                    new LinkServer(Host, port);
                }
            }

            catch (Exception)
            {
                throw new Exception("Failed to start tunnel. Please try again!");
            }
        }
        public static bool isRunning()
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            return Process.GetProcessesByName(processName).Length > 1;
        }
        public static string getIP()
        {
            IPHostEntry entry = Dns.GetHostEntry(Host);

            return entry.AddressList.Length > 0 ? entry.AddressList[0].ToString() : Host;
        }
    }
}
