using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace GFLauncher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls12;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LauncherForm());
        }
    }

    public class LauncherForm : Form
    {
        private const string RepoOwner = "KlevertonKulmann1";
        private const string RepoName = "GrandFantasia";
        private const string RegisterUrl = "http://26.96.179.219/index.php";

        private const int CanvasW = 1000;
        private const int CanvasH = 547;

        private readonly Rectangle rectStart = new Rectangle(322, 385, 382, 162);
        private readonly Rectangle rectStartHit = new Rectangle(370, 415, 286, 95);
        private readonly Rectangle rectClose = new Rectangle(638, 68, 80, 40);

        private const int VerifyBaseX = 36;
        private const int VerifyY = 398;
        private const int VerifyW = 287;
        private const int VerifyH = 69;

        private const int OptionsBaseX = 72;
        private const int OptionsY = 458;
        private const int OptionsW = 287;
        private const int OptionsH = 69;

        private const int RegisterBaseX = 682;
        private const int RegisterY = 398;
        private const int RegisterW = 287;
        private const int RegisterH = 70;

        private Image imgLauncher;
        private Image imgBtnDisabled;
        private Image imgBtnEnabled;
        private Image imgBtnHover;
        private Image imgBtnVerify;
        private Image imgBtnOptions;
        private Image imgBtnRegister;
        private Image imgClose;
        private Image imgCloseHover;

        private bool readyToLaunch = false;
        private int progress = 0;
        private string statusText = "Conectando ao servidor de atualizações...";
        private string activeBranch = "master";

        private bool hoverStart = false;
        private bool hoverVerify = false;
        private bool hoverOptions = false;
        private bool hoverRegister = false;
        private bool hoverClose = false;

        private float verifyOffset = 0f;
        private float optionsOffset = 0f;
        private float registerOffset = 0f;
        private readonly Timer animTimer;

        private bool isDragging = false;
        private Point dragCursorStart;
        private Point dragFormStart;

        private readonly string baseDir;

        public LauncherForm()
        {
            baseDir = AppDomain.CurrentDomain.BaseDirectory;
            try { File.Delete(Path.Combine(baseDir, "GFLauncher.exe.new")); } catch { }
            Text = "Grand Fantasia - Celestia";
            ClientSize = new Size(CanvasW, CanvasH);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(1, 3, 1);
            TransparencyKey = Color.FromArgb(1, 3, 1);
            DoubleBuffered = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            LoadAssets();

            animTimer = new Timer();
            animTimer.Interval = 16;
            animTimer.Tick += OnAnimTick;
            animTimer.Start();

            MouseMove += OnMouseMoveCanvas;
            MouseDown += OnMouseDownCanvas;
            MouseUp += OnMouseUpCanvas;
            MouseLeave += delegate
            {
                if (hoverStart || hoverVerify || hoverOptions || hoverRegister || hoverClose)
                {
                    hoverStart = hoverVerify = hoverOptions = hoverRegister = hoverClose = false;
                    Cursor = Cursors.Default;
                    Invalidate();
                }
            };
            Paint += OnPaintCanvas;
            Shown += delegate { CheckForUpdatesAsync(false); };
        }

        private void OnAnimTick(object sender, EventArgs e)
        {
            float targetVerify = hoverVerify ? -14f : 0f;
            float targetOptions = hoverOptions ? -14f : 0f;
            float targetRegister = hoverRegister ? 14f : 0f;

            bool changed = false;
            float nextV = StepTowards(verifyOffset, targetVerify, 2.5f);
            if (Math.Abs(nextV - verifyOffset) > 0.01f) { verifyOffset = nextV; changed = true; }

            float nextO = StepTowards(optionsOffset, targetOptions, 2.5f);
            if (Math.Abs(nextO - optionsOffset) > 0.01f) { optionsOffset = nextO; changed = true; }

            float nextR = StepTowards(registerOffset, targetRegister, 2.5f);
            if (Math.Abs(nextR - registerOffset) > 0.01f) { registerOffset = nextR; changed = true; }

            if (changed) Invalidate();
        }

        private static float StepTowards(float current, float target, float speed)
        {
            float diff = target - current;
            if (Math.Abs(diff) <= 0.4f) return target;
            return current + diff * 0.25f;
        }

        private Image LoadAsset(string name, bool required)
        {
            string p1 = Path.Combine(Path.Combine(Path.Combine(baseDir, "launcher_src"), "assets"), name);
            string p2 = Path.Combine(Path.Combine(baseDir, "assets"), name);
            string diskPath = File.Exists(p1) ? p1 : (File.Exists(p2) ? p2 : null);
            if (diskPath != null)
            {
                using (var fs = new FileStream(diskPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return Image.FromStream(fs);
                }
            }

            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            using (var rs = asm.GetManifestResourceStream("GFLauncher.assets." + name))
            {
                if (rs != null)
                {
                    return Image.FromStream(rs);
                }
            }

            if (required) throw new FileNotFoundException("Asset not found: " + name);
            return null;
        }

        private void LoadAssets()
        {
            try
            {
                imgLauncher = LoadAsset("launcher.png", true);
                imgBtnDisabled = LoadAsset("btn-disabled.png", true);
                imgBtnEnabled = LoadAsset("btn-enabled.png", true);
                imgBtnHover = LoadAsset("btn-hover.png", true);
                imgBtnVerify = LoadAsset("btn-verify.png", true);
                imgBtnOptions = LoadAsset("btn-options.png", true);
                imgBtnRegister = LoadAsset("btn-register.png", false);
                imgClose = LoadAsset("close.png", true);
                imgCloseHover = LoadAsset("close-hover.png", true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Rectangle GetVerifyHitRect()
        {
            return new Rectangle(VerifyBaseX + (int)Math.Round(verifyOffset), VerifyY, VerifyW, VerifyH);
        }

        private Rectangle GetOptionsHitRect()
        {
            return new Rectangle(OptionsBaseX + (int)Math.Round(optionsOffset), OptionsY, OptionsW, OptionsH);
        }

        private Rectangle GetRegisterHitRect()
        {
            return new Rectangle(RegisterBaseX + (int)Math.Round(registerOffset), RegisterY, RegisterW, RegisterH);
        }

        private void OnPaintCanvas(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Camada de fundo: painel escuro usando o poligono exato de masc.svg (470x365)
            using (var bgBrush = new SolidBrush(Color.FromArgb(232, 15, 23, 42)))
            using (var maskPath = new GraphicsPath())
            {
                const float ox = 278f;
                const float oy = 72f;
                maskPath.AddPolygon(new PointF[]
                {
                    new PointF(ox + 0f, oy + 94f),
                    new PointF(ox + 43.5f, oy + 67.5f),
                    new PointF(ox + 87f, oy + 67.5f),
                    new PointF(ox + 160.5f, oy + 12f),
                    new PointF(ox + 230f, oy + 0f),
                    new PointF(ox + 315f, oy + 12f),
                    new PointF(ox + 385.5f, oy + 67.5f),
                    new PointF(ox + 422.75f, oy + 67.5f),
                    new PointF(ox + 470f, oy + 94f),
                    new PointF(ox + 470f, oy + 365f),
                    new PointF(ox + 0f, oy + 365f)
                });
                g.FillPath(bgBrush, maskPath);
            }

            var progRect = new Rectangle(280, 380, 468, 28);
            using (var progBg = new SolidBrush(Color.FromArgb(230, 18, 22, 34)))
            {
                g.FillRectangle(progBg, progRect);
            }

            if (progress > 0)
            {
                int fillW = (int)(progRect.Width * Math.Min(100, Math.Max(0, progress)) / 100.0);
                if (fillW > 0)
                {
                    var fillRect = new Rectangle(progRect.X + 2, progRect.Y + 2, Math.Max(1, fillW - 4), progRect.Height - 4);
                    using (var fillBrush = new LinearGradientBrush(fillRect, Color.FromArgb(255, 245, 210, 125), Color.FromArgb(255, 200, 148, 56), LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(fillBrush, fillRect);
                    }
                }
            }

            using (var titleFont = new Font("Segoe UI", 14f, FontStyle.Bold))
            using (var bodyFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
            using (var goldBrush = new SolidBrush(Color.FromArgb(255, 245, 210, 125)))
            using (var textBrush = new SolidBrush(Color.FromArgb(240, 226, 232, 240)))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
            {
                g.DrawString("Bem-vindo a Celestia", titleFont, goldBrush, new RectangleF(285, 175, 456, 32), sf);
                g.DrawString(statusText, bodyFont, textBrush, new RectangleF(300, 220, 426, 140), sf);
            }

            // 2. Botoes laterais (atras da moldura launcher.png, deslizando para fora no hover)
            if (imgBtnVerify != null)
            {
                g.DrawImage(imgBtnVerify, GetVerifyHitRect());
            }
            if (imgBtnOptions != null)
            {
                g.DrawImage(imgBtnOptions, GetOptionsHitRect());
            }
            if (imgBtnRegister != null)
            {
                g.DrawImage(imgBtnRegister, GetRegisterHitRect());
            }

            // 3. Moldura principal do launcher
            if (imgLauncher != null)
            {
                g.DrawImage(imgLauncher, new Rectangle(100, 0, 800, 547));
            }

            // 4. Botao fechar
            Image closeImg = hoverClose ? imgCloseHover : imgClose;
            if (closeImg != null)
            {
                g.DrawImage(closeImg, rectClose);
            }

            // 5. Camada superior a tudo: botao INICIAR (disabled / enabled / hover)
            Image startImg = !readyToLaunch ? imgBtnDisabled : (hoverStart ? imgBtnHover : imgBtnEnabled);
            if (startImg != null)
            {
                g.DrawImage(startImg, rectStart);
            }
        }

        private void OnMouseMoveCanvas(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point diff = new Point(Cursor.Position.X - dragCursorStart.X, Cursor.Position.Y - dragCursorStart.Y);
                Location = new Point(dragFormStart.X + diff.X, dragFormStart.Y + diff.Y);
                return;
            }

            bool ns = readyToLaunch && rectStartHit.Contains(e.Location);
            bool nv = GetVerifyHitRect().Contains(e.Location);
            bool no = GetOptionsHitRect().Contains(e.Location);
            bool nr = imgBtnRegister != null && GetRegisterHitRect().Contains(e.Location);
            bool nc = rectClose.Contains(e.Location);

            if (ns != hoverStart || nv != hoverVerify || no != hoverOptions || nr != hoverRegister || nc != hoverClose)
            {
                hoverStart = ns;
                hoverVerify = nv;
                hoverOptions = no;
                hoverRegister = nr;
                hoverClose = nc;
                Cursor = (ns || nv || no || nr || nc) ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        private void OnMouseDownCanvas(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (rectClose.Contains(e.Location))
            {
                Close();
                return;
            }
            if (readyToLaunch && rectStartHit.Contains(e.Location))
            {
                LaunchGame();
                return;
            }
            if (GetVerifyHitRect().Contains(e.Location))
            {
                CheckForUpdatesAsync(true);
                return;
            }
            if (GetOptionsHitRect().Contains(e.Location))
            {
                OpenOptionsDialog();
                return;
            }
            if (imgBtnRegister != null && GetRegisterHitRect().Contains(e.Location))
            {
                try
                {
                    Process.Start(RegisterUrl);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao abrir navegador: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            isDragging = true;
            dragCursorStart = Cursor.Position;
            dragFormStart = Location;
        }

        private void OnMouseUpCanvas(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void SetStatus(string text, int prog)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(delegate { SetStatus(text, prog); }));
                return;
            }
            statusText = text;
            progress = prog;
            Invalidate();
        }

        private void SetReady(bool ready)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(delegate { SetReady(ready); }));
                return;
            }
            readyToLaunch = ready;
            Invalidate();
        }

        private static string ComputeGitBlobSha(string filePath)
        {
            byte[] data = File.ReadAllBytes(filePath);
            byte[] header = Encoding.ASCII.GetBytes("blob " + data.Length + "\0");
            using (var sha1 = new SHA1Managed())
            {
                sha1.TransformBlock(header, 0, header.Length, null, 0);
                sha1.TransformFinalBlock(data, 0, data.Length);
                var sb = new StringBuilder(40);
                foreach (byte b in sha1.Hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static bool ShouldDownload(string path)
        {
            string lower = path.ToLowerInvariant();
            if (lower == ".gitignore" || lower == "readme.md" || lower == "gflauncher.exe" || lower == "launcher_version.txt")
                return false;
            if (lower.StartsWith("launcher_src/"))
                return false;
            return true;
        }

        private void CheckForUpdatesAsync(bool forceFull)
        {
            SetReady(false);
            SetStatus("Conectando ao GitHub...\nVerificando se há novas traduções ou ajustes disponíveis.", 10);

            Task.Run(delegate
            {
                try
                {
                    using (var wc = new WebClient())
                    {
                        wc.Encoding = Encoding.UTF8;
                        wc.Headers.Add("User-Agent", "GFLauncher/2.0");
                        var js = new JavaScriptSerializer();
                        js.MaxJsonLength = int.MaxValue;

                        string remoteSha = null;
                        try
                        {
                            string refsData = wc.DownloadString(string.Format("https://github.com/{0}/{1}.git/info/refs?service=git-upload-pack", RepoOwner, RepoName));
                            Match rm = Regex.Match(refsData, @"([0-9a-f]{40})\s+refs/heads/(master|main)");
                            if (rm.Success)
                            {
                                remoteSha = rm.Groups[1].Value;
                                activeBranch = rm.Groups[2].Value;
                            }
                        }
                        catch { }

                        if (string.IsNullOrEmpty(remoteSha))
                        {
                            foreach (string br in new[] { "master", "main" })
                            {
                                try
                                {
                                    string commitJson = wc.DownloadString(string.Format("https://api.github.com/repos/{0}/{1}/commits/{2}", RepoOwner, RepoName, br));
                                    var commitObj = js.Deserialize<Dictionary<string, object>>(commitJson);
                                    remoteSha = Convert.ToString(commitObj["sha"]);
                                    activeBranch = br;
                                    break;
                                }
                                catch { }
                            }
                        }

                        if (string.IsNullOrEmpty(remoteSha))
                        {
                            SetStatus("Modo offline: não foi possível consultar atualizações.\nVocê ainda pode clicar em INICIAR.", 100);
                            SetReady(true);
                            return;
                        }

                        string verFile = Path.Combine(baseDir, "launcher_version.txt");
                        string localSha = File.Exists(verFile) ? File.ReadAllText(verFile).Trim() : "";

                        if (!forceFull && string.Equals(localSha, remoteSha, StringComparison.OrdinalIgnoreCase))
                        {
                            SetStatus("Tudo atualizado! Clique em INICIAR para jogar.\nServidor conectado via Celestia Launcher.", 100);
                            SetReady(true);
                            return;
                        }

                        var filesToDownload = new List<string>();
                        string remoteLauncherSha = null;
                        if (!forceFull && !string.IsNullOrEmpty(localSha))
                        {
                            SetStatus("Buscando arquivos modificados...", 30);
                            try
                            {
                                string cmpJson = wc.DownloadString(string.Format("https://api.github.com/repos/{0}/{1}/compare/{2}...{3}", RepoOwner, RepoName, localSha, remoteSha));
                                var cmpObj = js.Deserialize<Dictionary<string, object>>(cmpJson);
                                var files = cmpObj["files"] as ArrayList;
                                if (files != null)
                                {
                                    foreach (Dictionary<string, object> f in files)
                                    {
                                        string st = Convert.ToString(f["status"]);
                                        string fn = Convert.ToString(f["filename"]);
                                        if (st != "removed")
                                        {
                                            if (fn.Equals("GFLauncher.exe", StringComparison.OrdinalIgnoreCase))
                                                remoteLauncherSha = Convert.ToString(f["sha"]);
                                            else if (ShouldDownload(fn))
                                                filesToDownload.Add(fn);
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                forceFull = true;
                            }
                        }
                        else
                        {
                            forceFull = true;
                        }

                        if (forceFull)
                        {
                            SetStatus("Listando arquivos do repositório...", 30);
                            string treeJson = wc.DownloadString(string.Format("https://api.github.com/repos/{0}/{1}/git/trees/{2}?recursive=1", RepoOwner, RepoName, activeBranch));
                            var treeObj = js.Deserialize<Dictionary<string, object>>(treeJson);
                            var tree = treeObj["tree"] as ArrayList;
                            if (tree != null)
                            {
                                foreach (Dictionary<string, object> item in tree)
                                {
                                    if (Convert.ToString(item["type"]) == "blob")
                                    {
                                        string p = Convert.ToString(item["path"]);
                                        if (p.Equals("GFLauncher.exe", StringComparison.OrdinalIgnoreCase))
                                            remoteLauncherSha = Convert.ToString(item["sha"]);
                                        else if (ShouldDownload(p))
                                            filesToDownload.Add(p);
                                    }
                                }
                            }
                        }

                        int total = filesToDownload.Count;
                        for (int i = 0; i < total; i++)
                        {
                            string rel = filesToDownload[i];
                            string dest = Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
                            if (rel.Equals("client.ini", StringComparison.OrdinalIgnoreCase) && File.Exists(dest))
                                continue;

                            int pct = 35 + (int)(60.0 * (i + 1) / Math.Max(1, total));
                            SetStatus(string.Format("Baixando ({0}/{1}): {2}", i + 1, total, rel), pct);

                            string dir = Path.GetDirectoryName(dest);
                            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                            string url = string.Format("https://raw.githubusercontent.com/{0}/{1}/{2}/{3}", RepoOwner, RepoName, activeBranch, Uri.EscapeUriString(rel));
                            wc.DownloadFile(url, dest);
                        }

                        File.WriteAllText(verFile, remoteSha);

                        if (!string.IsNullOrEmpty(remoteLauncherSha))
                        {
                            string curExe = Path.Combine(baseDir, "GFLauncher.exe");
                            string localExeSha = File.Exists(curExe) ? ComputeGitBlobSha(curExe) : "";
                            if (!string.Equals(localExeSha, remoteLauncherSha, StringComparison.OrdinalIgnoreCase))
                            {
                                SetStatus("Atualizando o launcher...", 98);
                                string newExe = Path.Combine(baseDir, "GFLauncher.exe.new");
                                string exeUrl = string.Format("https://raw.githubusercontent.com/{0}/{1}/{2}/GFLauncher.exe", RepoOwner, RepoName, activeBranch);
                                wc.DownloadFile(exeUrl, newExe);
                                if (File.Exists(newExe) && new FileInfo(newExe).Length > 100000)
                                {
                                    var psi = new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -n 2 > nul & move /y \"GFLauncher.exe.new\" \"GFLauncher.exe\" & start \"\" \"GFLauncher.exe\"");
                                    psi.WorkingDirectory = baseDir;
                                    psi.CreateNoWindow = true;
                                    psi.UseShellExecute = false;
                                    Process.Start(psi);
                                    BeginInvoke(new Action(delegate { Application.Exit(); }));
                                    return;
                                }
                            }
                        }

                        SetStatus("Atualização concluída! Pronto para jogar.\nTodos os arquivos estão sincronizados.", 100);
                        SetReady(true);
                    }
                }
                catch (Exception ex)
                {
                    SetStatus("Modo offline: " + ex.Message + "\nVocê ainda pode clicar em INICIAR.", 100);
                    SetReady(true);
                }
            });
        }

        private void LaunchGame()
        {
            string exe = Path.Combine(baseDir, "GrandFantasia.exe");
            if (!File.Exists(exe))
            {
                MessageBox.Show("GrandFantasia.exe não encontrado nesta pasta!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                SetStatus("Iniciando Grand Fantasia - Celestia...", 100);
                var psi = new ProcessStartInfo(exe, "EasyFun");
                psi.WorkingDirectory = baseDir;
                Process.Start(psi);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao iniciar o jogo: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenOptionsDialog()
        {
            string iniPath = Path.Combine(baseDir, "client.ini");
            string iniText = File.Exists(iniPath) ? File.ReadAllText(iniPath, Encoding.Default) : "[Option]\r\n";

            Func<string, int, int> getInt = delegate(string key, int def)
            {
                Match m = Regex.Match(iniText, "(?m)^" + Regex.Escape(key) + @"\s*=\s*(-?\d+)");
                int v;
                return (m.Success && int.TryParse(m.Groups[1].Value, out v)) ? v : def;
            };
            Func<string, double, double> getDbl = delegate(string key, double def)
            {
                Match m = Regex.Match(iniText, "(?m)^" + Regex.Escape(key) + @"\s*=\s*([0-9]+(?:\.[0-9]+)?)");
                double v;
                return (m.Success && double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v)) ? v : def;
            };

            using (var dlg = new Form())
            {
                dlg.Text = "Opções - Grand Fantasia Celestia";
                dlg.Size = new Size(380, 420);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.StartPosition = FormStartPosition.CenterParent;

                var tabs = new TabControl { Dock = DockStyle.Top, Height = 325 };
                var tabDisp = new TabPage("Display");
                var tabAudio = new TabPage("Áudio");
                tabs.TabPages.Add(tabDisp);
                tabs.TabPages.Add(tabAudio);

                var chkWin = new CheckBox { Text = "Modo Janela", Left = 20, Top = 18, Width = 140, Checked = getInt("FullScreenMode", 1) == 0 };
                var chkOpt = new CheckBox { Text = "Otimizar (Ativar)", Left = 180, Top = 18, Width = 150, Checked = getInt("DynamicVideoSetting", 1) == 1 };
                tabDisp.Controls.Add(chkWin);
                tabDisp.Controls.Add(chkOpt);

                var lblRes = new Label { Text = "Resolução:", Left = 20, Top = 58, Width = 90 };
                var cmbRes = new ComboBox { Left = 120, Top = 55, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
                cmbRes.Items.AddRange(new object[] { "1024x768", "1280x720", "1280x960", "1366x768", "1600x900", "1920x1080" });
                string curRes = getInt("ScreenWidth", 1024) + "x" + getInt("ScreenHeight", 768);
                if (!cmbRes.Items.Contains(curRes)) cmbRes.Items.Add(curRes);
                cmbRes.SelectedItem = curRes;
                tabDisp.Controls.Add(lblRes);
                tabDisp.Controls.Add(cmbRes);

                var lblShad = new Label { Text = "Sombras:", Left = 20, Top = 98, Width = 90 };
                var cmbShad = new ComboBox { Left = 120, Top = 95, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
                cmbShad.Items.AddRange(new object[] { "Não", "Você", "Tudo" });
                cmbShad.SelectedIndex = Math.Min(2, Math.Max(0, getInt("ShadowLevel", 2)));
                tabDisp.Controls.Add(lblShad);
                tabDisp.Controls.Add(cmbShad);

                var lblFps = new Label { Text = "FPS Máx:", Left = 20, Top = 138, Width = 90 };
                var cmbFps = new ComboBox { Left = 120, Top = 135, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
                cmbFps.Items.AddRange(new object[] { "30", "60", "120" });
                string curFps = getInt("FpsLockValue", 60).ToString();
                if (!cmbFps.Items.Contains(curFps)) cmbFps.Items.Add(curFps);
                cmbFps.SelectedItem = curFps;
                tabDisp.Controls.Add(lblFps);
                tabDisp.Controls.Add(cmbFps);

                var chkMute = new CheckBox { Text = "Mudo (Desativar Áudio)", Left = 20, Top = 24, Width = 220, Checked = getInt("SoundMute", 0) == 1 };
                var lblBgm = new Label { Text = "Música (%):", Left = 20, Top = 68, Width = 100 };
                var trkBgm = new TrackBar { Left = 120, Top = 62, Width = 200, Minimum = 0, Maximum = 100, TickFrequency = 10, Value = Math.Min(100, Math.Max(0, (int)(getDbl("BGMValoume", 1.0) * 100))) };
                var lblSfx = new Label { Text = "SFX (%):", Left = 20, Top = 120, Width = 100 };
                var trkSfx = new TrackBar { Left = 120, Top = 114, Width = 200, Minimum = 0, Maximum = 100, TickFrequency = 10, Value = Math.Min(100, Math.Max(0, (int)(getDbl("SoundValoume", 1.0) * 100))) };
                tabAudio.Controls.Add(chkMute);
                tabAudio.Controls.Add(lblBgm);
                tabAudio.Controls.Add(trkBgm);
                tabAudio.Controls.Add(lblSfx);
                tabAudio.Controls.Add(trkSfx);

                var btnCancel = new Button { Text = "Cancelar", Left = 190, Top = 338, Width = 80, DialogResult = DialogResult.Cancel };
                var btnSave = new Button { Text = "Salvar", Left = 280, Top = 338, Width = 80, DialogResult = DialogResult.OK };
                dlg.Controls.Add(tabs);
                dlg.Controls.Add(btnCancel);
                dlg.Controls.Add(btnSave);
                dlg.AcceptButton = btnSave;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    Action<string, string> setKey = delegate(string k, string v)
                    {
                        string pat = "(?m)^(" + Regex.Escape(k) + @"\s*=\s*)[^\r\n]*";
                        if (Regex.IsMatch(iniText, pat))
                            iniText = Regex.Replace(iniText, pat, "${1}" + v);
                        else
                            iniText += (iniText.EndsWith("\n") ? "" : "\r\n") + k + "=" + v + "\r\n";
                    };

                    string[] wh = Convert.ToString(cmbRes.SelectedItem).Split('x');
                    setKey("FullScreenMode", chkWin.Checked ? "0" : "1");
                    setKey("DynamicVideoSetting", chkOpt.Checked ? "1" : "0");
                    if (wh.Length == 2)
                    {
                        setKey("ScreenWidth", wh[0]);
                        setKey("ScreenHeight", wh[1]);
                    }
                    setKey("ShadowLevel", cmbShad.SelectedIndex.ToString());
                    setKey("FpsLockValue", Convert.ToString(cmbFps.SelectedItem));
                    setKey("SoundMute", chkMute.Checked ? "1" : "0");
                    setKey("BGMValoume", (trkBgm.Value / 100.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
                    setKey("SoundValoume", (trkSfx.Value / 100.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
                    File.WriteAllText(iniPath, iniText, Encoding.Default);
                }
            }
        }
    }
}
