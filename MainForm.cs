using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MTUOptimizer
{
    public partial class MainForm : Form
    {
        private BackgroundWorker backgroundWorker;
        private List<MTUTestResult> testResults;
        private bool isTesting = false;

        public MainForm()
        {
            try
            {
                InitializeComponent();
                InitializeBackgroundWorker();
                testResults = new List<MTUTestResult>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در راه‌اندازی برنامه: {ex.Message}", "خطای بحرانی", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void InitializeBackgroundWorker()
        {
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        }

        private void InitializeComponent()
        {
            this.Text = "بهینه‌ساز MTU - MTU Optimizer";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Tahoma", 9F);

            try
            {
                // Create controls
                CreateControls();
                LayoutControls();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در راه‌اندازی رابط کاربری: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void CreateControls()
        {
            // Target IP/Host
            lblTarget = new Label
            {
                Text = "آدرس هدف (Target IP/Host):",
                Location = new Point(20, 20),
                Size = new Size(150, 20)
            };

            txtTarget = new TextBox
            {
                Location = new Point(180, 20),
                Size = new Size(200, 20),
                Text = "8.8.8.8"
            };

            // MTU Range
            lblMTURange = new Label
            {
                Text = "محدوده MTU:",
                Location = new Point(20, 50),
                Size = new Size(150, 20)
            };

            lblMinMTU = new Label
            {
                Text = "حداقل:",
                Location = new Point(180, 50),
                Size = new Size(40, 20)
            };

            numMinMTU = new NumericUpDown
            {
                Location = new Point(230, 50),
                Size = new Size(60, 20),
                Minimum = 68,
                Maximum = 1500,
                Value = 68
            };

            lblMaxMTU = new Label
            {
                Text = "حداکثر:",
                Location = new Point(300, 50),
                Size = new Size(40, 20)
            };

            numMaxMTU = new NumericUpDown
            {
                Location = new Point(350, 50),
                Size = new Size(60, 20),
                Minimum = 68,
                Maximum = 1500,
                Value = 1500
            };

            // Test Settings
            lblPingCount = new Label
            {
                Text = "تعداد پینگ برای هر MTU:",
                Location = new Point(20, 80),
                Size = new Size(150, 20)
            };

            numPingCount = new NumericUpDown
            {
                Location = new Point(180, 80),
                Size = new Size(60, 20),
                Minimum = 1,
                Maximum = 20,
                Value = 5
            };

            // Buttons
            btnStart = new Button
            {
                Text = "شروع تست",
                Location = new Point(20, 110),
                Size = new Size(100, 30),
                BackColor = Color.LightGreen
            };
            btnStart.Click += BtnStart_Click;

            btnStop = new Button
            {
                Text = "توقف",
                Location = new Point(130, 110),
                Size = new Size(100, 30),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            btnStop.Click += BtnStop_Click;

            // Progress
            lblProgress = new Label
            {
                Text = "آماده برای شروع",
                Location = new Point(20, 150),
                Size = new Size(400, 20)
            };

            progressBar = new ProgressBar
            {
                Location = new Point(20, 170),
                Size = new Size(400, 20)
            };

            // Results
            lblResults = new Label
            {
                Text = "نتایج:",
                Location = new Point(20, 200),
                Size = new Size(100, 20),
                Font = new Font("Tahoma", 9F, FontStyle.Bold)
            };

            listViewResults = new ListView
            {
                Location = new Point(20, 220),
                Size = new Size(750, 300),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            listViewResults.Columns.Add("MTU", 80);
            listViewResults.Columns.Add("میانگین پینگ (ms)", 120);
            listViewResults.Columns.Add("کمترین پینگ (ms)", 120);
            listViewResults.Columns.Add("بیشترین پینگ (ms)", 120);
            listViewResults.Columns.Add("انحراف معیار", 100);
            listViewResults.Columns.Add("وضعیت", 80);

            // Best Result
            lblBestResult = new Label
            {
                Text = "بهترین MTU: -",
                Location = new Point(20, 530),
                Size = new Size(400, 20),
                Font = new Font("Tahoma", 9F, FontStyle.Bold),
                ForeColor = Color.Green
            };

            // Add controls to form
            this.Controls.AddRange(new Control[]
            {
                lblTarget, txtTarget,
                lblMTURange, lblMinMTU, numMinMTU, lblMaxMTU, numMaxMTU,
                lblPingCount, numPingCount,
                btnStart, btnStop,
                lblProgress, progressBar,
                lblResults, listViewResults,
                lblBestResult
            });
        }

        private void LayoutControls()
        {
            // Right-to-left layout for Persian text
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTarget.Text))
                {
                    MessageBox.Show("لطفاً آدرس هدف را وارد کنید.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (numMinMTU.Value >= numMaxMTU.Value)
                {
                    MessageBox.Show("حداقل MTU باید کمتر از حداکثر باشد.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                StartTesting();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در شروع تست: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            try
            {
                if (backgroundWorker.IsBusy)
                {
                    backgroundWorker.CancelAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در توقف تست: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartTesting()
        {
            try
            {
                isTesting = true;
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                testResults.Clear();
                listViewResults.Items.Clear();
                lblBestResult.Text = "بهترین MTU: در حال تست...";
                progressBar.Value = 0;

                var testParams = new MTUTestParameters
                {
                    Target = txtTarget.Text.Trim(),
                    MinMTU = (int)numMinMTU.Value,
                    MaxMTU = (int)numMaxMTU.Value,
                    PingCount = (int)numPingCount.Value
                };

                // اضافه کردن تست اولیه برای اطمینان از اتصال
                Console.WriteLine($"شروع تست برای {testParams.Target} با محدوده MTU {testParams.MinMTU}-{testParams.MaxMTU}");
                
                backgroundWorker.RunWorkerAsync(testParams);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در شروع تست: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUI();
            }
        }

        private void ResetUI()
        {
            isTesting = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            progressBar.Value = 0;
            lblProgress.Text = "آماده برای شروع";
        }

        private async void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var parameters = (MTUTestParameters)e.Argument;
                var worker = (BackgroundWorker)sender;

                // Test MTU values in steps
                int step = Math.Max(1, (parameters.MaxMTU - parameters.MinMTU) / 20); // کاهش تعداد تست‌ها
                
                int totalTests = 0;
                int completedTests = 0;
                
                // محاسبه تعداد کل تست‌ها
                for (int mtu = parameters.MinMTU; mtu <= parameters.MaxMTU; mtu += step)
                {
                    totalTests++;
                }
                
                for (int mtu = parameters.MinMTU; mtu <= parameters.MaxMTU; mtu += step)
                {
                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    try
                    {
                        var result = await TestMTU(parameters.Target, mtu, parameters.PingCount, worker);
                        worker.ReportProgress(0, result);
                        
                        completedTests++;
                        int progress = (int)((completedTests * 100.0) / totalTests);
                        worker.ReportProgress(progress, null);
                        
                        // اضافه کردن تأخیر بین تست‌ها
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"خطا در تست MTU {mtu}: {ex.Message}");
                        var errorResult = new MTUTestResult
                        {
                            MTU = mtu,
                            AveragePing = -1,
                            MinPing = -1,
                            MaxPing = -1,
                            StandardDeviation = -1,
                            Status = "خطا: " + ex.Message
                        };
                        worker.ReportProgress(0, errorResult);
                    }
                }
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private async Task<MTUTestResult> TestMTU(string target, int mtu, int pingCount, BackgroundWorker worker)
        {
            try
            {
                if (worker.CancellationPending)
                {
                    return new MTUTestResult
                    {
                        MTU = mtu,
                        AveragePing = -1,
                        MinPing = -1,
                        MaxPing = -1,
                        StandardDeviation = -1,
                        Status = "متوقف شد"
                    };
                }

                return await TestMTUWithPing(target, mtu, pingCount);
            }
            catch (Exception ex)
            {
                return new MTUTestResult
                {
                    MTU = mtu,
                    AveragePing = -1,
                    MinPing = -1,
                    MaxPing = -1,
                    StandardDeviation = -1,
                    Status = "خطا: " + ex.Message
                };
            }
        }

        private async Task<MTUTestResult> TestMTUWithPing(string target, int mtu, int pingCount)
        {
            var pingTimes = new List<long>();
            var ping = new Ping();
            var dataSize = mtu - 28; // IP header (20) + ICMP header (8) = 28 bytes

            if (dataSize < 0) dataSize = 0;

            try
            {
                var data = new byte[dataSize];
                new Random().NextBytes(data); // Fill with random data

                for (int i = 0; i < pingCount; i++)
                {
                    try
                    {
                        var reply = await ping.SendPingAsync(target, 3000, data, new PingOptions
                        {
                            DontFragment = true,
                            Ttl = 128
                        });

                        if (reply.Status == IPStatus.Success)
                        {
                            pingTimes.Add(reply.RoundtripTime);
                        }
                        else if (reply.Status == IPStatus.PacketTooBig)
                        {
                            // MTU is too large, packet was fragmented
                            return new MTUTestResult
                            {
                                MTU = mtu,
                                AveragePing = -1,
                                MinPing = -1,
                                MaxPing = -1,
                                StandardDeviation = -1,
                                Status = "بسته خیلی بزرگ"
                            };
                        }
                        else
                        {
                            // Log other status types
                            Console.WriteLine($"Ping status for MTU {mtu}: {reply.Status}");
                        }

                        await Task.Delay(200); // Wait between pings
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"خطا در پینگ {i + 1} برای MTU {mtu}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطا در تست MTU {mtu}: {ex.Message}");
                return new MTUTestResult
                {
                    MTU = mtu,
                    AveragePing = -1,
                    MinPing = -1,
                    MaxPing = -1,
                    StandardDeviation = -1,
                    Status = "خطا: " + ex.Message
                };
            }

            if (pingTimes.Count == 0)
            {
                return new MTUTestResult
                {
                    MTU = mtu,
                    AveragePing = -1,
                    MinPing = -1,
                    MaxPing = -1,
                    StandardDeviation = -1,
                    Status = "ناموفق"
                };
            }

            return new MTUTestResult
            {
                MTU = mtu,
                AveragePing = pingTimes.Average(),
                MinPing = pingTimes.Min(),
                MaxPing = pingTimes.Max(),
                StandardDeviation = CalculateStandardDeviation(pingTimes),
                Status = "موفق"
            };
        }

        private double CalculateStandardDeviation(List<long> values)
        {
            if (values.Count <= 1) return 0;

            double mean = values.Average();
            double sumOfSquares = values.Sum(x => Math.Pow(x - mean, 2));
            return Math.Sqrt(sumOfSquares / (values.Count - 1));
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                if (e.UserState is MTUTestResult result)
                {
                    testResults.Add(result);
                    AddResultToListView(result);
                }

                if (e.ProgressPercentage >= 0)
                {
                    progressBar.Value = e.ProgressPercentage;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطا در به‌روزرسانی پیشرفت: {ex.Message}");
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                isTesting = false;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                progressBar.Value = 100;

                if (e.Cancelled)
                {
                    lblProgress.Text = "تست متوقف شد";
                    lblBestResult.Text = "بهترین MTU: تست متوقف شد";
                }
                else if (e.Error != null)
                {
                    lblProgress.Text = "خطا در تست: " + e.Error.Message;
                    lblBestResult.Text = "بهترین MTU: خطا";
                    MessageBox.Show($"خطا در تست: {e.Error.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    lblProgress.Text = "تست کامل شد";
                    FindBestMTU();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در تکمیل تست: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetUI();
            }
        }

        private void AddResultToListView(MTUTestResult result)
        {
            try
            {
                var item = new ListViewItem(result.MTU.ToString());
                item.SubItems.Add(result.AveragePing >= 0 ? result.AveragePing.ToString("F1") : "-");
                item.SubItems.Add(result.MinPing >= 0 ? result.MinPing.ToString() : "-");
                item.SubItems.Add(result.MaxPing >= 0 ? result.MaxPing.ToString() : "-");
                item.SubItems.Add(result.StandardDeviation >= 0 ? result.StandardDeviation.ToString("F1") : "-");
                item.SubItems.Add(result.Status);

                if (result.AveragePing >= 0)
                {
                    item.BackColor = Color.LightGreen;
                }
                else
                {
                    item.BackColor = Color.LightCoral;
                }

                listViewResults.Items.Add(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطا در اضافه کردن نتیجه: {ex.Message}");
            }
        }

        private void FindBestMTU()
        {
            try
            {
                var validResults = testResults.Where(r => r.AveragePing >= 0).ToList();
                
                if (validResults.Count == 0)
                {
                    lblBestResult.Text = "بهترین MTU: هیچ نتیجه معتبری یافت نشد";
                    return;
                }

                // Find the MTU with the lowest average ping
                var bestResult = validResults.OrderBy(r => r.AveragePing).First();
                
                lblBestResult.Text = $"بهترین MTU: {bestResult.MTU} (میانگین پینگ: {bestResult.AveragePing:F1}ms)";
                
                // Highlight the best result in the list
                foreach (ListViewItem item in listViewResults.Items)
                {
                    if (item.Text == bestResult.MTU.ToString())
                    {
                        item.BackColor = Color.Yellow;
                        item.EnsureVisible();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                lblBestResult.Text = "خطا در پیدا کردن بهترین MTU";
                Console.WriteLine($"خطا در پیدا کردن بهترین MTU: {ex.Message}");
            }
        }

        // Control declarations
        private Label lblTarget;
        private TextBox txtTarget;
        private Label lblMTURange;
        private Label lblMinMTU;
        private NumericUpDown numMinMTU;
        private Label lblMaxMTU;
        private NumericUpDown numMaxMTU;
        private Label lblPingCount;
        private NumericUpDown numPingCount;
        private Button btnStart;
        private Button btnStop;
        private Label lblProgress;
        private ProgressBar progressBar;
        private Label lblResults;
        private ListView listViewResults;
        private Label lblBestResult;
    }

    public class MTUTestParameters
    {
        public string Target { get; set; }
        public int MinMTU { get; set; }
        public int MaxMTU { get; set; }
        public int PingCount { get; set; }
    }

    public class MTUTestResult
    {
        public int MTU { get; set; }
        public double AveragePing { get; set; }
        public long MinPing { get; set; }
        public long MaxPing { get; set; }
        public double StandardDeviation { get; set; }
        public string Status { get; set; }
    }
}