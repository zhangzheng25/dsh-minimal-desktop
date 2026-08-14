using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace DshTray
{
    static class Program
    {
        const string PipeName = "dsh-tray-open";
        const string MutexName = "dsh-tray-single-instance";
        const string IconResName = "dshTrayIcon.ico";
        const int Port = 3080;
        const string EdgeUrl = "http://127.0.0.1:3080";

        static NotifyIcon tray;
        static bool spawnedByUs;
        static int serverPid;
        static bool exiting;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    NotifyExisting();
                    return;
                }

                Thread serverThread = new Thread(PipeServerLoop);
                serverThread.IsBackground = true;
                serverThread.Start();

                InitTray();
                Startup();

                Application.Run();
                if (tray != null) tray.Visible = false;
            }
        }

        static void NotifyExisting()
        {
            try
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(2000);
                    byte[] buf = Encoding.UTF8.GetBytes("open");
                    client.Write(buf, 0, buf.Length);
                }
            }
            catch { }
        }

        static void PipeServerLoop()
        {
            while (!exiting)
            {
                try
                {
                    using (NamedPipeServerStream server = new NamedPipeServerStream(PipeName, PipeDirection.In))
                    {
                        server.WaitForConnection();
                        byte[] buf = new byte[64];
                        int n = server.Read(buf, 0, buf.Length);
                        if (n > 0 && Encoding.UTF8.GetString(buf, 0, n) == "open")
                            EnsureServiceAndOpen();
                    }
                }
                catch { Thread.Sleep(200); }
            }
        }

        static void InitTray()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem open = new ToolStripMenuItem("打开 DSh");
            open.Click += delegate { EnsureServiceAndOpen(); };
            ToolStripMenuItem restart = new ToolStripMenuItem("重启服务");
            restart.Click += delegate { RestartService(); };
            ToolStripMenuItem exit = new ToolStripMenuItem("退出");
            exit.Click += delegate { ExitApp(); };
            menu.Items.Add(open);
            menu.Items.Add(restart);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exit);

            tray = new NotifyIcon();
            tray.Icon = LoadIcon();
            tray.Text = "DSh 服务 (127.0.0.1:" + Port + ")";
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
            tray.MouseClick += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) EnsureServiceAndOpen();
            };
        }

        static Icon LoadIcon()
        {
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(IconResName))
                {
                    if (s != null) return new Icon(s);
                }
            }
            catch { }
            return SystemIcons.Application;
        }

        static void Startup()
        {
            if (!PortBusy(Port))
            {
                StartServer();
                WaitReadyThenOpen();
            }
            else
            {
                OpenBrowser();
            }
        }

        static void EnsureServiceAndOpen()
        {
            if (!PortBusy(Port))
            {
                StartServer();
                WaitReadyThenOpen();
            }
            else
            {
                OpenBrowser();
            }
        }

        static bool restarting;

        static void RestartService()
        {
            Thread t = new Thread(RestartServiceCore);
            t.IsBackground = true;
            t.Start();
        }

        static void RestartServiceCore()
        {
            if (restarting) return;
            restarting = true;
            try
            {
                Log("restart requested");
                KillPortOwner(Port);

                // wait until the port is actually released before respawning
                DateTime deadline = DateTime.Now.AddSeconds(10);
                while (DateTime.Now < deadline && PortBusy(Port) && !exiting)
                    Thread.Sleep(200);
                if (exiting) return;

                if (StartServer())
                {
                    if (tray != null)
                        tray.ShowBalloonTip(2500, "DSh 服务", "服务正在重启 (端口 " + Port + ") ...", ToolTipIcon.Info);
                    WaitReadyThenOpen();
                }
                else
                {
                    Log("restart failed");
                    if (tray != null)
                        tray.ShowBalloonTip(2500, "DSh 服务", "服务重启失败，请查看 logs\\app.log", ToolTipIcon.Error);
                }
            }
            finally
            {
                restarting = false;
            }
        }

        static void KillPortOwner(int port)
        {
            // first kill the process we spawned ourselves (if any)
            KillServerTree();

            // then kill whatever else is actually listening on the port
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("netstat", "-ano");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(3000);

                    Regex re = new Regex("\\s+TCP\\s+[^\\s]+:" + port + "\\s+[^\\s]+\\s+LISTENING\\s+(\\d+)");
                    foreach (Match m in re.Matches(output))
                    {
                        int pid;
                        if (!int.TryParse(m.Groups[1].Value, out pid) || pid == 0) continue;
                        if (pid == Process.GetCurrentProcess().Id) continue;
                        ProcessStartInfo kpsi = new ProcessStartInfo("taskkill", "/PID " + pid + " /T /F");
                        kpsi.CreateNoWindow = true;
                        kpsi.UseShellExecute = false;
                        using (Process kp = Process.Start(kpsi)) kp.WaitForExit(3000);
                        Log("port owner killed, pid=" + pid);
                    }
                }
            }
            catch (Exception ex) { Log("kill port owner error: " + ex.Message); }
            serverPid = 0;
            spawnedByUs = false;
        }

        static void WaitReadyThenOpen()
        {
            Thread t = new Thread(delegate()
            {
                DateTime deadline = DateTime.Now.AddSeconds(90);
                while (DateTime.Now < deadline && !exiting)
                {
                    if (HttpReady(Port))
                    {
                        OpenBrowser();
                        return;
                    }
                    Thread.Sleep(400);
                }
                Log("ready wait timeout");
            });
            t.IsBackground = true;
            t.Start();
        }

        static bool StartServer()
        {
            if (spawnedByUs && serverPid != 0)
                KillServerTree();

            string node = ResolveNode();
            string dshBin = ResolveDshBin();
            string args;
            if (dshBin != null)
                args = "\"" + dshBin + "\" web --port " + Port;
            else
                args = "--yes @deepseek-ai/dsh web --port " + Port;

            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir);
                string logFile = Path.Combine(logDir, "server.log");

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = node;
                psi.Arguments = args;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                Process p = Process.Start(psi);
                if (p == null) { Log("spawn failed"); return false; }
                serverPid = p.Id;
                spawnedByUs = true;
                p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
                {
                    if (e.Data != null)
                        File.AppendAllText(logFile, e.Data + Environment.NewLine);
                };
                p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
                {
                    if (e.Data != null)
                        File.AppendAllText(logFile, e.Data + Environment.NewLine);
                };
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                Log("server spawned, pid=" + serverPid + ", dsh=" + dshBin);
                return true;
            }
            catch (Exception ex)
            {
                Log("spawn error: " + ex.Message);
                return false;
            }
        }

        static void OpenBrowser()
        {
            try
            {
                Process.Start("msedge", "--app=" + EdgeUrl);
                Log("opened edge app window");
            }
            catch (Exception ex)
            {
                Log("open browser error: " + ex.Message);
            }
        }

        static void KillServerTree()
        {
            if (serverPid == 0) return;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("taskkill", "/PID " + serverPid + " /T /F");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                using (Process p = Process.Start(psi)) p.WaitForExit(3000);
                Log("server tree killed, pid=" + serverPid);
            }
            catch (Exception ex) { Log("kill error: " + ex.Message); }
            serverPid = 0;
            spawnedByUs = false;
        }

        static void ExitApp()
        {
            exiting = true;
            KillServerTree();
            Application.Exit();
        }

        static bool PortBusy(int port)
        {
            try
            {
                TcpClient client = new TcpClient();
                IAsyncResult ar = client.BeginConnect("127.0.0.1", port, null, null);
                bool connected = ar.AsyncWaitHandle.WaitOne(600) && client.Connected;
                client.Close();
                return connected;
            }
            catch { return false; }
        }

        static bool HttpReady(int port)
        {
            try
            {
                TcpClient client = new TcpClient();
                IAsyncResult ar = client.BeginConnect("127.0.0.1", port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(1500) || !client.Connected)
                {
                    client.Close();
                    return false;
                }
                NetworkStream ns = client.GetStream();
                byte[] req = Encoding.ASCII.GetBytes("GET / HTTP/1.0\r\nHost: 127.0.0.1\r\n\r\n");
                ns.Write(req, 0, req.Length);
                byte[] buf = new byte[64];
                int n = ns.Read(buf, 0, buf.Length);
                client.Close();
                if (n <= 0) return false;
                string line = Encoding.ASCII.GetString(buf, 0, n);
                return line.StartsWith("HTTP/1.") && line.IndexOf(" 200 ") >= 0;
            }
            catch { return false; }
        }

        static string ResolveNode()
        {
            string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "nodejs", "node.exe");
            return File.Exists(p) ? p : "node";
        }

        static string ResolveDshBin()
        {
            string best = null;
            string bestVer = null;
            string npxRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "npm-cache", "_npx");
            if (Directory.Exists(npxRoot))
            {
                foreach (string d in Directory.GetDirectories(npxRoot))
                {
                    string pkg = Path.Combine(d, "node_modules", "@deepseek-ai", "dsh");
                    string bin = Path.Combine(pkg, "lib", "bin.js");
                    if (File.Exists(bin))
                    {
                        string ver = ReadVersion(Path.Combine(pkg, "package.json"));
                        if (best == null || CompareVersions(ver, bestVer) > 0)
                        {
                            best = bin;
                            bestVer = ver;
                        }
                    }
                }
            }
            return best;
        }

        static string ReadVersion(string pkgJson)
        {
            try
            {
                string s = File.ReadAllText(pkgJson);
                Match m = Regex.Match(s, "\"version\"\\s*:\\s*\"([^\"]+)\"");
                return m.Success ? m.Groups[1].Value : "";
            }
            catch { return ""; }
        }

        static int[] ParseVersion(string v)
        {
            Match m = Regex.Match(v ?? "", "^(\\d+)\\.(\\d+)\\.(\\d+)(?:-rc(\\d+))?");
            if (!m.Success) return new int[] { 0, 0, 0, int.MaxValue };
            int rc = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : int.MaxValue;
            return new int[] { int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value), rc };
        }

        static int CompareVersions(string a, string b)
        {
            int[] x = ParseVersion(a), y = ParseVersion(b);
            for (int i = 0; i < 4; i++)
                if (x[i] != y[i]) return x[i].CompareTo(y[i]);
            return 0;
        }

        static void Log(string msg)
        {
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir);
                File.AppendAllText(Path.Combine(logDir, "app.log"),
                    DateTime.Now.ToString("HH:mm:ss") + " " + msg + Environment.NewLine);
            }
            catch { }
        }
    }
}
