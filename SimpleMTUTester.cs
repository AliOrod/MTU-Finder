 using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MTUOptimizer
{
    public partial class SimpleMTUTester : Form
    {
        private List<MTUTestResult> testResults;
        private bool isTesting = false;

        public SimpleMTUTester()
        {
            InitializeComponent();
            testResults = new List<MTUTestResult>();
        }

        private void InitializeComponent()
        {
            this.Text = "تست ساده MTU - Simple MTU Tester";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Tahoma", 9F);

            CreateControls();
            LayoutControls();
        }

        private void CreateControls()
        {
            // Target IP/Host
            lblTarget = new Label
            {
                Text = "آدرس هدف:",
                Location = new Point(20, 20),
                Size = new Size(100, 20)
            };

            txtTarget = new TextBox
            {
                Location = new Point(130, 20),
                Size = new Size(150, 20),
                Text = "8.8.8.8"
            };

            // MTU Range
            lblMTURange = new Label
            {
                Text = "محدوده MTU:",
                Location = new Point(20, 50),
                Size = new Size(100, 20)
            };

            numMinMTU = new NumericUpDown
            {
                Location = new Point(130, 50),
                Size = new Size(60, 20),
                Minimum = 68,
                Maximum = 1500,
                Value = 68
            };

            numMaxMTU = new NumericUpDown
            {
                Location = new Point(200, 50),
                Size = new Size(60, 20),
                Minimum = 68,
                Maximum = 1500,
                Value = 1500
            };

            // Ping Count
            lblPingCount = new Label
            {
                Text = "تعداد پینگ:",
                Location = new Point(20, 80),
                Size = new Size(100, 20)
            };

            numPingCount = new NumericUpDown
            {
                Location = new Point(130, 80),
                Size = new Size(60, 20),
                Minimum = 1,
                Maximum = 10,
                Value = 3
            };

            // Step Size
            lblStep = new Label
            {
                Text = "گام تست:",
                Location = new Point(20, 110),
                Size = new Size(100, 20)
            };

            numStep = new NumericUpDown
            {
                Location = new Point(130, 110),
                Size = new Size(60, 20),
                Minimum = 1,
                Maximum = 100,
                Value = 1
            };

            // Buttons
            btnStart = new Button
            {
                Text = "شروع تست",
                Location = new Point(20, 140),
                Size = new Size(100, 30),
                BackColor = Color.LightGreen
            };
            btnStart.Click += BtnStart_Click;

            btnStop = new Button
            {
                Text = "توقف",
                Location = new Point(130, 140),
                Size = new Size(100, 30),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            btnStop.Click += BtnStop_Click;

            // Progress
            lblProgress = new Label
            {
                Text = "آماده برای شروع",
                Location = new Point(20, 180),
                Size = new Size(300, 20)
            };

            progressBar = new ProgressBar
            {
                Location = new Point(20, 200),
                Size = new Size(300, 20)
            };

            // Results
            lblResults = new Label
            {
                Text = "نتایج:",
                Location = new Point(20, 230),
                Size = new Size(100, 20),
                Font = new Font("Tahoma", 9F, FontStyle.Bold)
            };

            listViewResults = new ListView
            {
                Location = new Point(20, 250),
                Size = new Size(550, 170),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            listViewResults.Columns.Add("MTU", 60);
            listViewResults.Columns.Add("میانگین پینگ", 100);
            listViewResults.Columns.Add("وضعیت", 80);

            // Best Result
            lblBestResult = new Label
            {
                Text = "بهترین MTU: -",
                Location = new Point(20, 430),
                Size = new Size(300, 20),
                Font = new Font("Tahoma", 9F, FontStyle.Bold),
                ForeColor = Color.Green
            };

            // Add controls
            this.Controls.AddRange(new Control[]
            {
                lblTarget, txtTarget,
                lblMTURange, numMinMTU, numMaxMTU,
                lblPingCount, numPingCount,
                lblStep, numStep,
                btnStart, btnStop,
                lblProgress, progressBar,
                lblResults, listViewResults,
                lblBestResult
            });
        }

        private void LayoutControls()
        {
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
        }

        private async void BtnStart_Click(object sender, EventArgs e)
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

            await StartTesting();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            isTesting = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblProgress.Text = "تست متوقف شد";
        }

        private async Task StartTesting()
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

                var target = txtTarget.Text.Trim();
                var minMTU = (int)numMinMTU.Value;
                var maxMTU = (int)numMaxMTU.Value;
                var pingCount = (int)numPingCount.Value;
                var step = (int)numStep.Value;

                Console.WriteLine($"شروع تست برای {target} با محدوده MTU {minMTU}-{maxMTU} و گام {step}");

                // Test MTU values
                int totalTests = 0;
                int completedTests = 0;

                // Count total tests
                for (int mtu = minMTU; mtu <= maxMTU; mtu += step)
                {
                    totalTests++;
                }

                for (int mtu = minMTU; mtu <= maxMTU; mtu += step)
                {
                    if (!isTesting)
                    {
                        break;
                    }

                    try
                    {
                        lblProgress.Text = $"در حال تست MTU {mtu}...";
                        progressBar.Value = (completedTests * 100) / totalTests;

                        var result = await TestMTU(target, mtu, pingCount);
                        testResults.Add(result);
                        AddResultToListView(result);

                        completedTests++;
                        await Task.Delay(100); // Small delay between tests
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
                        testResults.Add(errorResult);
                        AddResultToListView(errorResult);
                    }
                }

                if (isTesting)
                {
                    lblProgress.Text = "تست کامل شد";
                    progressBar.Value = 100;
                    FindBestMTU();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در تست: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isTesting = false;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
        }

        private async Task<MTUTestResult> TestMTU(string target, int mtu, int pingCount)
        {
            var pingTimes = new List<long>();
            var ping = new Ping();
            var dataSize = mtu - 28;

            if (dataSize < 0) dataSize = 0;

            try
            {
                var data = new byte[dataSize];
                new Random().NextBytes(data);

                for (int i = 0; i < pingCount; i++)
                {
                    if (!isTesting) break;

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
                            Console.WriteLine($"MTU {mtu}, Ping {i + 1}: {reply.RoundtripTime}ms");
                        }
                        else
                        {
                            Console.WriteLine($"MTU {mtu}, Status: {reply.Status}");
                        }

                        await Task.Delay(200);
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

        private void AddResultToListView(MTUTestResult result)
        {
            try
            {
                var item = new ListViewItem(result.MTU.ToString());
                item.SubItems.Add(result.AveragePing >= 0 ? result.AveragePing.ToString("F1") : "-");
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

                var bestResult = validResults.OrderBy(r => r.AveragePing).First();
                lblBestResult.Text = $"بهترین MTU: {bestResult.MTU} (میانگین پینگ: {bestResult.AveragePing:F1}ms)";
                
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
        private NumericUpDown numMinMTU;
        private NumericUpDown numMaxMTU;
        private Label lblPingCount;
        private NumericUpDown numPingCount;
        private Label lblStep;
        private NumericUpDown numStep;
        private Button btnStart;
        private Button btnStop;
        private Label lblProgress;
        private ProgressBar progressBar;
        private Label lblResults;
        private ListView listViewResults;
        private Label lblBestResult;
    }
}