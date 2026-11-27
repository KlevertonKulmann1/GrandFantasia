using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: AssemblyTitle("Grand Fantasia Celestia Launcher")]
[assembly: AssemblyDescription("Launcher oficial do servidor Grand Fantasia Celestia")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Grand Fantasia Celestia")]
[assembly: AssemblyProduct("GFLauncher")]
[assembly: AssemblyCopyright("Copyright © 2026 Grand Fantasia Celestia")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

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
        private float startHoverAnim = 0f;
        private float closeHoverAnim = 0f;
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

            float targetStart = (readyToLaunch && hoverStart) ? 1f : 0f;
            float nextS = OptionsForm.StepAnim(startHoverAnim, targetStart, 0.30f);
            if (Math.Abs(nextS - startHoverAnim) > 0.001f) { startHoverAnim = nextS; changed = true; }

            float targetClose = hoverClose ? 1f : 0f;
            float nextC = OptionsForm.StepAnim(closeHoverAnim, targetClose, 0.30f);
            if (Math.Abs(nextC - closeHoverAnim) > 0.001f) { closeHoverAnim = nextC; changed = true; }

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

            // 4. Botao fechar com transicao suave e escala 1.03 no hover
            OptionsForm.DrawScaledHoverImage(g, imgClose, imgCloseHover, rectClose, closeHoverAnim);

            // 5. Camada superior a tudo: botao INICIAR (disabled / enabled / hover com escala 1.03)
            if (!readyToLaunch)
            {
                if (imgBtnDisabled != null) g.DrawImage(imgBtnDisabled, rectStart);
            }
            else
            {
                OptionsForm.DrawScaledHoverImage(g, imgBtnEnabled, imgBtnHover, rectStart, startHoverAnim);
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
            if (lower == ".gitignore" || lower == "readme.md" || lower == "gflauncher.exe" || lower == "launcher_version.txt" || lower == "launcher_manifest.txt")
                return false;
            if (lower.StartsWith("launcher_src/") || lower.StartsWith(".github/"))
                return false;
            return true;
        }

        private void CheckForUpdatesAsync(bool forceFull)
        {
            SetReady(false);
            SetStatus("Conectando ao servidor de atualizações...\nVerificando se há novas traduções ou ajustes disponíveis.", 10);

            Task.Run(delegate
            {
                try
                {
                    using (var wc = new WebClient())
                    {
                        wc.Encoding = Encoding.UTF8;
                        wc.Headers.Add("User-Agent", "GFLauncher/2.0");

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

                        SetStatus("Baixando lista de arquivos...", 25);
                        string manifestUrl = string.Format("https://raw.githubusercontent.com/{0}/{1}/{2}/launcher_manifest.txt?t={3}", RepoOwner, RepoName, activeBranch, DateTime.UtcNow.Ticks);
                        string manifestText = wc.DownloadString(manifestUrl);

                        var filesToDownload = new List<string>();
                        string remoteLauncherSha = null;
                        string[] lines = manifestText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        for (int idx = 0; idx < lines.Length; idx++)
                        {
                            string line = lines[idx];
                            int sep = line.IndexOf('|');
                            if (sep <= 0) continue;
                            string expectedSha = line.Substring(0, sep).Trim();
                            string rel = line.Substring(sep + 1).Trim();

                            if (rel.Equals("GFLauncher.exe", StringComparison.OrdinalIgnoreCase))
                            {
                                remoteLauncherSha = expectedSha;
                                continue;
                            }
                            if (!ShouldDownload(rel)) continue;

                            string dest = Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
                            if (rel.Equals("client.ini", StringComparison.OrdinalIgnoreCase) && File.Exists(dest))
                                continue;

                            if (!File.Exists(dest))
                            {
                                filesToDownload.Add(rel);
                            }
                            else
                            {
                                if (idx % 25 == 0)
                                {
                                    int chkPct = 25 + (int)(10.0 * idx / Math.Max(1, lines.Length));
                                    SetStatus("Verificando integridade dos arquivos...", chkPct);
                                }
                                string curSha = ComputeGitBlobSha(dest);
                                if (!string.Equals(curSha, expectedSha, StringComparison.OrdinalIgnoreCase))
                                {
                                    filesToDownload.Add(rel);
                                }
                            }
                        }

                        int total = filesToDownload.Count;
                        for (int i = 0; i < total; i++)
                        {
                            string rel = filesToDownload[i];
                            string dest = Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));

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
                                string exeUrl = string.Format("https://raw.githubusercontent.com/{0}/{1}/{2}/GFLauncher.exe?t={3}", RepoOwner, RepoName, activeBranch, DateTime.UtcNow.Ticks);
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
            using (var dlg = new OptionsForm(iniPath))
            {
                dlg.ShowDialog(this);
            }
        }
    }

    internal sealed class OptionsForm : Form
    {
        private const int W = 494;
        private const int H = 712;

        private readonly string iniPath;
        private string iniText;

        private readonly Image imgBg, imgClose, imgCloseHover;
        private readonly Image imgTabDispActive, imgTabDispHover, imgTabDispOff;
        private readonly Image imgTabAudioActive, imgTabAudioHover, imgTabAudioOff;
        private readonly Image imgCheckOn, imgCheckOff;
        private readonly Image imgSelClose, imgSelOpen;
        private readonly Image imgSliderFrame, imgSliderProg, imgSliderThumb;
        private readonly Image imgCancelDef, imgCancelHover, imgCancelOff;
        private readonly Image imgSaveDef, imgSaveHover, imgSaveOff;
        private readonly Image imgScrollBg, imgScrollThumb;

        private int activeTab = 0; // 0 = Display, 1 = Audio
        private int hoverTab = -1;
        private bool dirty = false;

        // Display state
        private bool optWindowed;
        private bool optOptimize;
        private readonly string[] resOptions = new[] { "1024 x 768", "1280 x 720", "1280 x 960", "1366 x 768", "1600 x 900", "1920 x 1080" };
        private int resIndex;
        private readonly string[] qualityOptions = new[] { "Baixo", "Médio", "Alto" };
        private int qualityIndex; // 0..2 -> ScreenLevel
        private readonly string[] shadowOptions = new[] { "Não", "Você", "Tudo" };
        private int shadowIndex; // 0..2 -> ShadowLevel 1..3
        private readonly string[] fpsOptions = new[] { "30", "60", "120" };
        private int fpsIndex;
        private int valCharRange;  // 1..40
        private int valViewRange;  // 1..5
        private int valEffectNum;  // 1..25
        private int valShadowType; // 1..5
        private bool optCharTex;
        private bool optSceneTex;
        private bool optAniso;
        private bool optDof;
        private bool optMono;
        private bool optSepia;

        // Audio state
        private bool optMute;
        private int valBgm; // 0..100
        private int valSfx; // 0..100

        // UI interaction state
        private int openDropdown = -1; // -1 none, 0 res, 1 quality, 2 shadow, 3 fps
        private int hoverDropItem = -1;
        private int activeSlider = -1; // 0..3 in Display, 0..1 in Audio
        private int scrollY = 0;
        private int maxScrollY = 260;
        private bool draggingScrollbar = false;
        private int scrollDragOffsetY = 0;
        private bool hoverClose = false;
        private bool hoverCancel = false;
        private bool hoverSave = false;
        private float animClose = 0f;  // 0.0 .. 1.0
        private float animCancel = 0f; // 0.0 .. 1.0
        private float animSave = 0f;   // 0.0 .. 1.0
        private readonly Timer animTimer;
        private bool isDragging = false;
        private Point dragCursorStart;
        private Point dragFormStart;

        // Layout rects (baseado no artboard 988x1424 em escala 0.5x -> 494x712)
        private readonly Rectangle rectClose = new Rectangle(406, 12, 68, 34);
        private readonly Rectangle rectTabDisp = new Rectangle(124, 59, 116, 40);
        private readonly Rectangle rectTabAudio = new Rectangle(256, 59, 116, 40);

        private readonly Rectangle rectViewport = new Rectangle(40, 102, 384, 509);
        private readonly Rectangle rectScrollbarTrack = new Rectangle(428, 114, 10, 469);

        private readonly Rectangle rectChkWin = new Rectangle(76, 112, 36, 35);
        private readonly Rectangle rectChkWinHit = new Rectangle(76, 112, 165, 35);
        private readonly Rectangle rectChkOpt = new Rectangle(262, 112, 36, 35);
        private readonly Rectangle rectChkOptHit = new Rectangle(262, 112, 145, 35);

        private readonly Rectangle rectSelRes = new Rectangle(104, 186, 276, 44);
        private readonly Rectangle rectSelQuality = new Rectangle(104, 266, 276, 44);
        private readonly Rectangle rectSelShadow = new Rectangle(104, 346, 276, 44);
        private readonly Rectangle rectSelFps = new Rectangle(104, 426, 276, 44);

        private readonly Rectangle[] rectDispSliders = new[]
        {
            new Rectangle(180, 502, 232, 31),
            new Rectangle(180, 550, 232, 31),
            new Rectangle(180, 598, 232, 31),
            new Rectangle(180, 646, 232, 31)
        };

        private readonly Rectangle rectChkCharTex = new Rectangle(64, 696, 36, 35);
        private readonly Rectangle rectChkCharTexHit = new Rectangle(64, 696, 178, 35);
        private readonly Rectangle rectChkSceneTex = new Rectangle(252, 696, 36, 35);
        private readonly Rectangle rectChkSceneTexHit = new Rectangle(252, 696, 182, 35);

        private readonly Rectangle rectChkAniso = new Rectangle(64, 742, 36, 35);
        private readonly Rectangle rectChkAnisoHit = new Rectangle(64, 742, 178, 35);
        private readonly Rectangle rectChkDof = new Rectangle(252, 742, 36, 35);
        private readonly Rectangle rectChkDofHit = new Rectangle(252, 742, 182, 35);

        private readonly Rectangle rectChkMono = new Rectangle(64, 788, 36, 35);
        private readonly Rectangle rectChkMonoHit = new Rectangle(64, 788, 178, 35);
        private readonly Rectangle rectChkSepia = new Rectangle(252, 788, 36, 35);
        private readonly Rectangle rectChkSepiaHit = new Rectangle(252, 788, 182, 35);

        private readonly Rectangle rectChkMute = new Rectangle(138, 128, 36, 35);
        private readonly Rectangle rectChkMuteHit = new Rectangle(138, 128, 220, 35);
        private readonly Rectangle[] rectAudioSliders = new[]
        {
            new Rectangle(200, 214, 232, 31),
            new Rectangle(200, 278, 232, 31)
        };

        private readonly Rectangle rectBtnCancel = new Rectangle(68, 618, 165, 46);
        private readonly Rectangle rectBtnSave = new Rectangle(261, 618, 165, 46);

        private readonly Font fntTitle;
        private readonly Font fntLabel;
        private readonly Font fntValue;
        private readonly Font fntSmall;

        public OptionsForm(string clientIniPath)
        {
            iniPath = clientIniPath;
            iniText = File.Exists(iniPath) ? File.ReadAllText(iniPath, Encoding.Default) : "[Option]\r\n";

            Text = "Opções - Grand Fantasia Celestia";
            ClientSize = new Size(W, H);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(1, 3, 1);
            TransparencyKey = Color.FromArgb(1, 3, 1);
            DoubleBuffered = true;
            ShowInTaskbar = false;

            fntTitle = new Font("Georgia", 12.5f, FontStyle.Bold);
            fntLabel = new Font("Georgia", 11.5f, FontStyle.Regular);
            fntValue = new Font("Georgia", 10.5f, FontStyle.Regular);
            fntSmall = new Font("Georgia", 9.0f, FontStyle.Regular);

            imgBg = LoadRes("Subtract.png");
            imgClose = LoadRes("close.png");
            imgCloseHover = LoadRes("close-hover.png");
            imgTabDispActive = LoadRes("tab-display-active.png");
            imgTabDispHover = LoadRes("tab-display-hover.png");
            imgTabDispOff = LoadRes("tab-display-off.png");
            imgTabAudioActive = LoadRes("tab-audio-active.png");
            imgTabAudioHover = LoadRes("tab-audio-hover.png");
            imgTabAudioOff = LoadRes("tab-audio-off.png");
            imgCheckOn = LoadRes("check-on.png");
            imgCheckOff = LoadRes("check-off.png");
            imgSelClose = LoadRes("selector-close.png");
            imgSelOpen = LoadRes("selector-open.png");
            imgSliderFrame = LoadRes("slider-frame.png");
            imgSliderProg = LoadRes("slider-progress.png");
            imgSliderThumb = LoadRes("slider-thumb.png");
            imgCancelDef = LoadRes("btn-cancel-default.png");
            imgCancelHover = LoadRes("btn-cancel-hover.png");
            imgCancelOff = LoadRes("btn-cancel-off.png");
            imgSaveDef = LoadRes("btn-save-default.png");
            imgSaveHover = LoadRes("btn-save-hover.png");
            imgSaveOff = LoadRes("btn-save-off.png");
            imgScrollBg = LoadRes("scroll-background.png");
            imgScrollThumb = LoadRes("scroll-thumb.png");

            LoadIniValues();

            animTimer = new Timer();
            animTimer.Interval = 16;
            animTimer.Tick += OnAnimTickOpt;
            animTimer.Start();

            MouseDown += OnMouseDownOpt;
            MouseMove += OnMouseMoveOpt;
            MouseUp += OnMouseUpOpt;
            MouseWheel += OnMouseWheelOpt;
            MouseLeave += delegate
            {
                hoverTab = -1;
                hoverClose = hoverCancel = hoverSave = false;
                hoverDropItem = -1;
                Invalidate();
            };
            Paint += OnPaintOpt;
        }

        private void OnAnimTickOpt(object sender, EventArgs e)
        {
            bool changed = false;
            float tClose = hoverClose ? 1f : 0f;
            float tCancel = hoverCancel ? 1f : 0f;
            float tSave = (dirty && hoverSave) ? 1f : 0f;

            float nClose = StepAnim(animClose, tClose, 0.30f);
            float nCancel = StepAnim(animCancel, tCancel, 0.30f);
            float nSave = StepAnim(animSave, tSave, 0.30f);

            if (Math.Abs(nClose - animClose) > 0.001f) { animClose = nClose; changed = true; }
            if (Math.Abs(nCancel - animCancel) > 0.001f) { animCancel = nCancel; changed = true; }
            if (Math.Abs(nSave - animSave) > 0.001f) { animSave = nSave; changed = true; }

            if (changed) Invalidate();
        }

        internal static float StepAnim(float cur, float target, float step)
        {
            if (Math.Abs(target - cur) <= step) return target;
            return cur + (target > cur ? step : -step);
        }

        internal static void DrawScaledHoverImage(Graphics g, Image imgBase, Image imgHover, Rectangle baseRect, float t)
        {
            float scale = 1.0f + 0.03f * t;
            float dw = baseRect.Width * (scale - 1f);
            float dh = baseRect.Height * (scale - 1f);
            RectangleF r = new RectangleF(baseRect.X - dw * 0.5f, baseRect.Y - dh * 0.5f, baseRect.Width + dw, baseRect.Height + dh);

            if (imgBase != null && t < 0.999f)
                g.DrawImage(imgBase, r);

            if (imgHover != null && t > 0.001f)
            {
                if (t >= 0.999f)
                {
                    g.DrawImage(imgHover, r);
                }
                else
                {
                    using (var ia = new System.Drawing.Imaging.ImageAttributes())
                    {
                        var cm = new System.Drawing.Imaging.ColorMatrix();
                        cm.Matrix33 = t;
                        ia.SetColorMatrix(cm);
                        PointF[] destPts = new[]
                        {
                            new PointF(r.Left, r.Top),
                            new PointF(r.Right, r.Top),
                            new PointF(r.Left, r.Bottom)
                        };
                        g.DrawImage(imgHover, destPts, new RectangleF(0, 0, imgHover.Width, imgHover.Height), GraphicsUnit.Pixel, ia);
                    }
                }
            }
        }

        private static Image LoadRes(string name)
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream rs = asm.GetManifestResourceStream("GFLauncher.assets." + name))
            {
                if (rs == null) return null;
                using (var tmp = Image.FromStream(rs))
                {
                    return new Bitmap(tmp);
                }
            }
        }

        private int GetInt(string key, int def)
        {
            Match m = Regex.Match(iniText, "(?m)^" + Regex.Escape(key) + @"\s*=\s*(-?\d+)");
            int v;
            return (m.Success && int.TryParse(m.Groups[1].Value, out v)) ? v : def;
        }

        private double GetDbl(string key, double def)
        {
            Match m = Regex.Match(iniText, "(?m)^" + Regex.Escape(key) + @"\s*=\s*([0-9]+(?:\.[0-9]+)?)");
            double v;
            return (m.Success && double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v)) ? v : def;
        }

        private void LoadIniValues()
        {
            optWindowed = GetInt("FullScreenMode", 1) == 0;
            optOptimize = GetInt("DynamicVideoSetting", 1) == 1;

            string curRes = GetInt("ScreenWidth", 1920) + " x " + GetInt("ScreenHeight", 1080);
            resIndex = Array.IndexOf(resOptions, curRes);
            if (resIndex < 0) resIndex = resOptions.Length - 1;

            qualityIndex = Math.Min(2, Math.Max(0, GetInt("ScreenLevel", 2)));

            int shLvl = GetInt("ShadowLevel", 3);
            shadowIndex = Math.Min(2, Math.Max(0, shLvl - 1));

            string curFps = GetInt("FpsLockValue", 120).ToString();
            fpsIndex = Array.IndexOf(fpsOptions, curFps);
            if (fpsIndex < 0) fpsIndex = 2;

            valCharRange = Math.Min(40, Math.Max(1, GetInt("ViewCharacterRange", 20)));
            valViewRange = Math.Min(5, Math.Max(1, GetInt("ViewRange", 5)));
            valEffectNum = Math.Min(25, Math.Max(1, GetInt("CharacterEffectNum", 25)));
            valShadowType = Math.Min(5, Math.Max(1, GetInt("ShadowType", 5)));

            optCharTex = GetInt("CharacterTexture", 1) == 1;
            optSceneTex = GetInt("SceneTexture", 1) == 1;
            optAniso = GetInt("MaxAnisotropic", 1) == 1;
            optDof = GetInt("DepthOfField", 0) == 1;
            optMono = GetInt("PPMonochrome", 0) == 1;
            optSepia = GetInt("PPSepia", 0) == 1;

            optMute = GetInt("SoundMute", 0) == 1;
            valBgm = Math.Min(100, Math.Max(0, (int)Math.Round(GetDbl("BGMValoume", 1.0) * 100.0)));
            valSfx = Math.Min(100, Math.Max(0, (int)Math.Round(GetDbl("SoundValoume", 1.0) * 100.0)));
        }

        private void SaveIniValues()
        {
            Action<string, string> setKey = delegate(string k, string v)
            {
                string pat = "(?m)^(" + Regex.Escape(k) + @"\s*=\s*)[^\r\n]*";
                if (Regex.IsMatch(iniText, pat))
                    iniText = Regex.Replace(iniText, pat, "${1}" + v);
                else
                    iniText += (iniText.EndsWith("\n") ? "" : "\r\n") + k + "=" + v + "\r\n";
            };

            string[] wh = resOptions[resIndex].Replace(" ", "").Split('x');
            setKey("FullScreenMode", optWindowed ? "0" : "1");
            setKey("DynamicVideoSetting", optOptimize ? "1" : "0");
            if (wh.Length == 2)
            {
                setKey("ScreenWidth", wh[0]);
                setKey("ScreenHeight", wh[1]);
            }
            setKey("ScreenLevel", qualityIndex.ToString());
            setKey("ShadowLevel", (shadowIndex + 1).ToString());
            setKey("FpsLockValue", fpsOptions[fpsIndex]);
            setKey("ViewCharacterRange", valCharRange.ToString());
            setKey("ViewRange", valViewRange.ToString());
            setKey("CharacterEffectNum", valEffectNum.ToString());
            setKey("ShadowType", valShadowType.ToString());
            setKey("CharacterTexture", optCharTex ? "1" : "0");
            setKey("SceneTexture", optSceneTex ? "1" : "0");
            setKey("MaxAnisotropic", optAniso ? "1" : "0");
            setKey("DepthOfField", optDof ? "1" : "0");
            setKey("PPMonochrome", optMono ? "1" : "0");
            setKey("PPSepia", optSepia ? "1" : "0");

            setKey("SoundMute", optMute ? "1" : "0");
            setKey("BGMValoume", (valBgm / 100.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
            setKey("SoundValoume", (valSfx / 100.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
            File.WriteAllText(iniPath, iniText, Encoding.Default);
        }

        private string[] GetDropdownItems(int dd)
        {
            if (dd == 0) return resOptions;
            if (dd == 1) return qualityOptions;
            if (dd == 2) return shadowOptions;
            if (dd == 3) return fpsOptions;
            return new string[0];
        }

        private Rectangle GetDropdownBoxRect(int dd)
        {
            Rectangle r;
            if (dd == 0) r = rectSelRes;
            else if (dd == 1) r = rectSelQuality;
            else if (dd == 2) r = rectSelShadow;
            else r = rectSelFps;
            return OffsetY(r, -scrollY);
        }

        private static Rectangle OffsetY(Rectangle r, int dy)
        {
            return new Rectangle(r.X, r.Y + dy, r.Width, r.Height);
        }

        private Rectangle GetScrollThumbRect()
        {
            int thumbW = 34;
            int thumbH = 28;
            int trackH = rectScrollbarTrack.Height;
            float ratio = maxScrollY > 0 ? (float)scrollY / (float)maxScrollY : 0f;
            int thumbX = rectScrollbarTrack.X + (rectScrollbarTrack.Width - thumbW) / 2;
            int thumbY = rectScrollbarTrack.Y + (int)Math.Round((trackH - thumbH) * ratio);
            return new Rectangle(thumbX, thumbY, thumbW, thumbH);
        }

        private Rectangle GetDropdownMenuRect(int dd)
        {
            Rectangle box = GetDropdownBoxRect(dd);
            string[] items = GetDropdownItems(dd);
            int itemH = 26;
            return new Rectangle(box.X + 12, box.Bottom - 2, box.Width - 24, items.Length * itemH + 6);
        }

        private void OnPaintOpt(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Fundo azul marinho #111729 com cantos arredondados bem por dentro da moldura
            using (var brNavy = new SolidBrush(Color.FromArgb(0x11, 0x17, 0x29)))
            using (var gp = new GraphicsPath())
            {
                int rx = 34, ry = 52, rw = W - 68, rh = H - 116, rad = 24;
                int d = rad * 2;
                gp.AddArc(rx, ry, d, d, 180, 90);
                gp.AddArc(rx + rw - d, ry, d, d, 270, 90);
                gp.AddArc(rx + rw - d, ry + rh - d, d, d, 0, 90);
                gp.AddArc(rx, ry + rh - d, d, d, 90, 90);
                gp.CloseFigure();
                g.FillPath(brNavy, gp);
            }

            if (imgBg != null)
                g.DrawImage(imgBg, 0, 0, W, H);

            DrawScaledHoverImage(g, imgClose, imgCloseHover, rectClose, animClose);

            // Abas Display / Audio
            Image dispImg = activeTab == 0 ? imgTabDispActive : (hoverTab == 0 ? imgTabDispHover : imgTabDispOff);
            Image audioImg = activeTab == 1 ? imgTabAudioActive : (hoverTab == 1 ? imgTabAudioHover : imgTabAudioOff);
            if (dispImg != null) g.DrawImage(dispImg, rectTabDisp);
            if (audioImg != null) g.DrawImage(audioImg, rectTabAudio);

            using (var brGold = new SolidBrush(Color.FromArgb(232, 212, 138)))
            using (var brText = new SolidBrush(Color.FromArgb(238, 224, 185)))
            using (var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
            {
                if (activeTab == 0)
                {
                    GraphicsState clipState = g.Save();
                    g.SetClip(rectViewport);

                    DrawCheckbox(g, OffsetY(rectChkWin, -scrollY), optWindowed, "Modo Janela", brGold, sfLeft);
                    DrawCheckbox(g, OffsetY(rectChkOpt, -scrollY), optOptimize, "Otimizar", brGold, sfLeft);

                    DrawSelector(g, "Resolução", OffsetY(rectSelRes, -scrollY), resOptions[resIndex], openDropdown == 0, brGold, brText, sfCenter);
                    DrawSelector(g, "Qualidade Geral", OffsetY(rectSelQuality, -scrollY), qualityOptions[qualityIndex], openDropdown == 1, brGold, brText, sfCenter);
                    DrawSelector(g, "Sombras", OffsetY(rectSelShadow, -scrollY), shadowOptions[shadowIndex], openDropdown == 2, brGold, brText, sfCenter);
                    DrawSelector(g, "FPS Máximo", OffsetY(rectSelFps, -scrollY), fpsOptions[fpsIndex], openDropdown == 3, brGold, brText, sfCenter);

                    DrawSlider(g, "Dist. Personagens", valCharRange, 1, 40, valCharRange.ToString(), OffsetY(rectDispSliders[0], -scrollY), brGold, sfLeft);
                    DrawSlider(g, "Alcance de Visão", valViewRange, 1, 5, valViewRange.ToString(), OffsetY(rectDispSliders[1], -scrollY), brGold, sfLeft);
                    DrawSlider(g, "Efeitos", valEffectNum, 1, 25, valEffectNum.ToString(), OffsetY(rectDispSliders[2], -scrollY), brGold, sfLeft);
                    DrawSlider(g, "Qualidade Sombra", valShadowType, 1, 5, valShadowType.ToString(), OffsetY(rectDispSliders[3], -scrollY), brGold, sfLeft);

                    DrawCheckbox(g, OffsetY(rectChkCharTex, -scrollY), optCharTex, "Textura Pers. Alta", brGold, sfLeft, true);
                    DrawCheckbox(g, OffsetY(rectChkSceneTex, -scrollY), optSceneTex, "Textura Cenário Alta", brGold, sfLeft, true);
                    DrawCheckbox(g, OffsetY(rectChkAniso, -scrollY), optAniso, "Filtro Anisotrópico", brGold, sfLeft, true);
                    DrawCheckbox(g, OffsetY(rectChkDof, -scrollY), optDof, "Profund. de Campo", brGold, sfLeft, true);
                    DrawCheckbox(g, OffsetY(rectChkMono, -scrollY), optMono, "Monocromático", brGold, sfLeft, true);
                    DrawCheckbox(g, OffsetY(rectChkSepia, -scrollY), optSepia, "Sépia", brGold, sfLeft, true);

                    g.Restore(clipState);

                    if (imgScrollBg != null)
                        g.DrawImage(imgScrollBg, rectScrollbarTrack);
                    if (imgScrollThumb != null)
                        g.DrawImage(imgScrollThumb, GetScrollThumbRect());
                }
                else
                {
                    DrawCheckbox(g, rectChkMute, optMute, "Mudo (Desativar Áudio)", brGold, sfLeft);
                    DrawSlider(g, "Música", valBgm, 0, 100, valBgm + "%", rectAudioSliders[0], brGold, sfLeft);
                    DrawSlider(g, "SFX", valSfx, 0, 100, valSfx + "%", rectAudioSliders[1], brGold, sfLeft);
                }

                // Menu aberto por cima de tudo na area central
                if (openDropdown >= 0)
                {
                    DrawOpenDropdownMenu(g, openDropdown, sfCenter);
                }
            }

            // Botoes do rodape com transicao suave e escala 1.03 no hover
            DrawScaledHoverImage(g, imgCancelDef, imgCancelHover, rectBtnCancel, animCancel);
            if (!dirty)
            {
                if (imgSaveOff != null) g.DrawImage(imgSaveOff, rectBtnSave);
            }
            else
            {
                DrawScaledHoverImage(g, imgSaveDef, imgSaveHover, rectBtnSave, animSave);
            }
        }

        private void DrawCheckbox(Graphics g, Rectangle box, bool isChecked, string label, Brush brLabel, StringFormat sfLeft)
        {
            DrawCheckbox(g, box, isChecked, label, brLabel, sfLeft, false);
        }

        private void DrawCheckbox(Graphics g, Rectangle box, bool isChecked, string label, Brush brLabel, StringFormat sfLeft, bool compact)
        {
            Image img = isChecked ? imgCheckOn : imgCheckOff;
            if (img != null)
            {
                Rectangle drawRect = isChecked ? new Rectangle(box.X + 2, box.Y, box.Width, box.Height) : box;
                g.DrawImage(img, drawRect);
            }
            g.DrawString(label, compact ? fntSmall : fntLabel, brLabel, new RectangleF(box.Right + 6, box.Y, compact ? 150 : 200, box.Height), sfLeft);
        }

        private void DrawSelector(Graphics g, string title, Rectangle box, string valueText, bool isOpen, Brush brTitle, Brush brVal, StringFormat sfCenter)
        {
            g.DrawString(title, fntLabel, brTitle, new RectangleF(box.X, box.Y - 24, box.Width, 22), sfCenter);
            Image bg = isOpen ? imgSelOpen : imgSelClose;
            if (bg != null) g.DrawImage(bg, box);
            g.DrawString(valueText, fntValue, brVal, new RectangleF(box.X + 20, box.Y + 2, box.Width - 48, box.Height - 4), sfCenter);
        }

        private void DrawSlider(Graphics g, string label, int val, int min, int max, string valTxt, Rectangle frameRect, Brush brLabel, StringFormat sfLeft)
        {
            g.DrawString(label, fntLabel, brLabel, new RectangleF(54, frameRect.Y, 152, frameRect.Height), sfLeft);

            if (imgSliderFrame != null)
                g.DrawImage(imgSliderFrame, frameRect);

            float pct = (max > min) ? (float)(val - min) / (float)(max - min) : 0f;
            pct = Math.Max(0f, Math.Min(1f, pct));

            int trackX = frameRect.X + 14;
            int trackY = frameRect.Y + 6;
            int trackW = 204;
            int trackH = 19;

            if (imgSliderProg != null && pct > 0.001f)
            {
                int clipW = Math.Max(1, (int)Math.Round(trackW * pct));
                GraphicsState st = g.Save();
                g.SetClip(new Rectangle(trackX, trackY, clipW, trackH), CombineMode.Intersect);
                g.DrawImage(imgSliderProg, trackX, trackY, trackW, trackH);
                g.Restore(st);
            }

            int thumbCenterX = trackX + (int)Math.Round(trackW * pct);
            if (imgSliderThumb != null)
            {
                int thumbW = 33;
                int thumbH = 27;
                int thumbX = thumbCenterX - (thumbW / 2);
                int thumbY = frameRect.Y + 2;
                g.DrawImage(imgSliderThumb, thumbX, thumbY, thumbW, thumbH);
            }

            using (var brVal = new SolidBrush(Color.FromArgb(232, 212, 138)))
            using (var sfThumb = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(valTxt, fntSmall, brVal, new RectangleF(thumbCenterX - 24, frameRect.Y - 15, 48, 15), sfThumb);
            }
        }

        private void DrawOpenDropdownMenu(Graphics g, int dd, StringFormat sfCenter)
        {
            Rectangle menu = GetDropdownMenuRect(dd);
            string[] items = GetDropdownItems(dd);
            int curSel = dd == 0 ? resIndex : (dd == 1 ? qualityIndex : (dd == 2 ? shadowIndex : fpsIndex));

            using (var brMenu = new SolidBrush(Color.FromArgb(245, 13, 21, 36)))
            using (var penBorder = new Pen(Color.FromArgb(167, 139, 86), 1.5f))
            {
                g.FillRectangle(brMenu, menu);
                g.DrawRectangle(penBorder, menu);
            }

            for (int i = 0; i < items.Length; i++)
            {
                Rectangle itemR = new Rectangle(menu.X + 2, menu.Y + 3 + i * 26, menu.Width - 4, 24);
                if (i == hoverDropItem || i == curSel)
                {
                    using (var brHi = new SolidBrush(i == hoverDropItem ? Color.FromArgb(90, 167, 139, 86) : Color.FromArgb(50, 167, 139, 86)))
                        g.FillRectangle(brHi, itemR);
                }
                using (var brTxt = new SolidBrush(i == curSel ? Color.FromArgb(248, 224, 140) : Color.FromArgb(225, 225, 230)))
                {
                    g.DrawString(items[i], fntValue, brTxt, itemR, sfCenter);
                }
            }
        }

        private void UpdateSliderFromMouse(int sliderIdx, int mouseX)
        {
            Rectangle r = activeTab == 0 ? rectDispSliders[sliderIdx] : rectAudioSliders[sliderIdx];
            int trackX = r.X + 14;
            int trackW = 204;
            float pct = (float)(mouseX - trackX) / (float)trackW;
            pct = Math.Max(0f, Math.Min(1f, pct));

            if (activeTab == 0)
            {
                if (sliderIdx == 0) valCharRange = 1 + (int)Math.Round(pct * 39f);
                else if (sliderIdx == 1) valViewRange = 1 + (int)Math.Round(pct * 4f);
                else if (sliderIdx == 2) valEffectNum = 1 + (int)Math.Round(pct * 24f);
                else if (sliderIdx == 3) valShadowType = 1 + (int)Math.Round(pct * 4f);
            }
            else
            {
                if (sliderIdx == 0) valBgm = (int)Math.Round(pct * 100f);
                else if (sliderIdx == 1) valSfx = (int)Math.Round(pct * 100f);
            }
            dirty = true;
            Invalidate();
        }

        private void OnMouseDownOpt(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (rectClose.Contains(e.Location))
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            if (openDropdown >= 0)
            {
                Rectangle menu = GetDropdownMenuRect(openDropdown);
                if (menu.Contains(e.Location))
                {
                    int idx = (e.Y - (menu.Y + 3)) / 26;
                    string[] items = GetDropdownItems(openDropdown);
                    if (idx >= 0 && idx < items.Length)
                    {
                        if (openDropdown == 0) resIndex = idx;
                        else if (openDropdown == 1) qualityIndex = idx;
                        else if (openDropdown == 2) shadowIndex = idx;
                        else if (openDropdown == 3) fpsIndex = idx;
                        dirty = true;
                    }
                    openDropdown = -1;
                    Invalidate();
                    return;
                }
                int prevDd = openDropdown;
                openDropdown = -1;
                if (GetDropdownBoxRect(prevDd).Contains(e.Location))
                {
                    Invalidate();
                    return;
                }
            }

            if (rectTabDisp.Contains(e.Location))
            {
                activeTab = 0;
                openDropdown = -1;
                Invalidate();
                return;
            }
            if (rectTabAudio.Contains(e.Location))
            {
                activeTab = 1;
                openDropdown = -1;
                Invalidate();
                return;
            }

            if (rectBtnCancel.Contains(e.Location))
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }
            if (rectBtnSave.Contains(e.Location) && dirty)
            {
                SaveIniValues();
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            if (activeTab == 0)
            {
                Rectangle thumbR = GetScrollThumbRect();
                if (thumbR.Contains(e.Location) || rectScrollbarTrack.Contains(e.Location))
                {
                    draggingScrollbar = true;
                    scrollDragOffsetY = thumbR.Contains(e.Location) ? (e.Y - thumbR.Y) : (thumbR.Height / 2);
                    UpdateScrollFromMouseY(e.Y - scrollDragOffsetY);
                    return;
                }

                if (rectViewport.Contains(e.Location))
                {
                    Point pt = new Point(e.X, e.Y + scrollY);
                    if (rectChkWinHit.Contains(pt)) { optWindowed = !optWindowed; dirty = true; Invalidate(); return; }
                    if (rectChkOptHit.Contains(pt)) { optOptimize = !optOptimize; dirty = true; Invalidate(); return; }
                    if (rectSelRes.Contains(pt)) { openDropdown = 0; Invalidate(); return; }
                    if (rectSelQuality.Contains(pt)) { openDropdown = 1; Invalidate(); return; }
                    if (rectSelShadow.Contains(pt)) { openDropdown = 2; Invalidate(); return; }
                    if (rectSelFps.Contains(pt)) { openDropdown = 3; Invalidate(); return; }
                    for (int i = 0; i < rectDispSliders.Length; i++)
                    {
                        if (rectDispSliders[i].Contains(pt))
                        {
                            activeSlider = i;
                            UpdateSliderFromMouse(i, e.X);
                            return;
                        }
                    }
                    if (rectChkCharTexHit.Contains(pt)) { optCharTex = !optCharTex; dirty = true; Invalidate(); return; }
                    if (rectChkSceneTexHit.Contains(pt)) { optSceneTex = !optSceneTex; dirty = true; Invalidate(); return; }
                    if (rectChkAnisoHit.Contains(pt)) { optAniso = !optAniso; dirty = true; Invalidate(); return; }
                    if (rectChkDofHit.Contains(pt)) { optDof = !optDof; dirty = true; Invalidate(); return; }
                    if (rectChkMonoHit.Contains(pt)) { optMono = !optMono; dirty = true; Invalidate(); return; }
                    if (rectChkSepiaHit.Contains(pt)) { optSepia = !optSepia; dirty = true; Invalidate(); return; }
                }
            }
            else
            {
                if (rectChkMuteHit.Contains(e.Location)) { optMute = !optMute; dirty = true; Invalidate(); return; }
                for (int i = 0; i < rectAudioSliders.Length; i++)
                {
                    if (rectAudioSliders[i].Contains(e.Location))
                    {
                        activeSlider = i;
                        UpdateSliderFromMouse(i, e.X);
                        return;
                    }
                }
            }

            if (e.Y < 56)
            {
                isDragging = true;
                dragCursorStart = Cursor.Position;
                dragFormStart = Location;
            }
        }

        private void OnMouseMoveOpt(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point cur = Cursor.Position;
                Location = new Point(dragFormStart.X + (cur.X - dragCursorStart.X), dragFormStart.Y + (cur.Y - dragCursorStart.Y));
                return;
            }

            if (activeSlider >= 0)
            {
                UpdateSliderFromMouse(activeSlider, e.X);
                return;
            }

            if (draggingScrollbar)
            {
                UpdateScrollFromMouseY(e.Y - scrollDragOffsetY);
                return;
            }

            int nTab = rectTabDisp.Contains(e.Location) ? 0 : (rectTabAudio.Contains(e.Location) ? 1 : -1);
            bool nClose = rectClose.Contains(e.Location);
            bool nCancel = rectBtnCancel.Contains(e.Location);
            bool nSave = dirty && rectBtnSave.Contains(e.Location);

            int nDropHover = -1;
            if (openDropdown >= 0)
            {
                Rectangle menu = GetDropdownMenuRect(openDropdown);
                if (menu.Contains(e.Location))
                {
                    int idx = (e.Y - (menu.Y + 3)) / 26;
                    string[] items = GetDropdownItems(openDropdown);
                    if (idx >= 0 && idx < items.Length) nDropHover = idx;
                }
            }

            bool overInteractive = nTab >= 0 || nClose || nCancel || nSave || nDropHover >= 0;
            if (!overInteractive && activeTab == 0)
            {
                if (GetScrollThumbRect().Contains(e.Location) || rectScrollbarTrack.Contains(e.Location))
                {
                    overInteractive = true;
                }
                else if (rectViewport.Contains(e.Location))
                {
                    Point pt = new Point(e.X, e.Y + scrollY);
                    overInteractive = rectChkWinHit.Contains(pt) || rectChkOptHit.Contains(pt)
                        || rectSelRes.Contains(pt) || rectSelQuality.Contains(pt) || rectSelShadow.Contains(pt) || rectSelFps.Contains(pt)
                        || rectDispSliders[0].Contains(pt) || rectDispSliders[1].Contains(pt) || rectDispSliders[2].Contains(pt) || rectDispSliders[3].Contains(pt)
                        || rectChkCharTexHit.Contains(pt) || rectChkSceneTexHit.Contains(pt) || rectChkAnisoHit.Contains(pt)
                        || rectChkDofHit.Contains(pt) || rectChkMonoHit.Contains(pt) || rectChkSepiaHit.Contains(pt);
                }
            }
            else if (!overInteractive && activeTab == 1)
            {
                overInteractive = rectChkMuteHit.Contains(e.Location)
                    || rectAudioSliders[0].Contains(e.Location) || rectAudioSliders[1].Contains(e.Location);
            }

            Cursor = overInteractive ? Cursors.Hand : Cursors.Default;

            if (nTab != hoverTab || nClose != hoverClose || nCancel != hoverCancel || nSave != hoverSave || nDropHover != hoverDropItem)
            {
                hoverTab = nTab;
                hoverClose = nClose;
                hoverCancel = nCancel;
                hoverSave = nSave;
                hoverDropItem = nDropHover;
                Invalidate();
            }
        }

        private void OnMouseUpOpt(object sender, MouseEventArgs e)
        {
            isDragging = false;
            activeSlider = -1;
            draggingScrollbar = false;
        }

        private void OnMouseWheelOpt(object sender, MouseEventArgs e)
        {
            if (activeTab != 0 || openDropdown >= 0) return;
            int next = scrollY - (e.Delta / 120) * 32;
            scrollY = Math.Max(0, Math.Min(maxScrollY, next));
            Invalidate();
        }

        private void UpdateScrollFromMouseY(int thumbTopY)
        {
            Rectangle thumbR = GetScrollThumbRect();
            int usable = Math.Max(1, rectScrollbarTrack.Height - thumbR.Height);
            float ratio = (float)(thumbTopY - rectScrollbarTrack.Y) / (float)usable;
            ratio = Math.Max(0f, Math.Min(1f, ratio));
            scrollY = (int)Math.Round(ratio * maxScrollY);
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (animTimer != null)
                {
                    animTimer.Stop();
                    animTimer.Dispose();
                }
                Image[] imgs = new[]
                {
                    imgBg, imgClose, imgCloseHover, imgTabDispActive, imgTabDispHover, imgTabDispOff,
                    imgTabAudioActive, imgTabAudioHover, imgTabAudioOff,
                    imgCheckOn, imgCheckOff, imgSelClose, imgSelOpen,
                    imgSliderFrame, imgSliderProg, imgSliderThumb,
                    imgCancelDef, imgCancelHover, imgCancelOff,
                    imgSaveDef, imgSaveHover, imgSaveOff,
                    imgScrollBg, imgScrollThumb
                };
                for (int i = 0; i < imgs.Length; i++)
                {
                    if (imgs[i] != null) imgs[i].Dispose();
                }
                fntTitle.Dispose();
                fntLabel.Dispose();
                fntValue.Dispose();
                fntSmall.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
