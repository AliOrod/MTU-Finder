 using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace MTUOptimizer
{
    public class NetworkInterfaceHelper
    {
        public static List<NetworkInterfaceInfo> GetNetworkInterfaces()
        {
            var interfaces = new List<NetworkInterfaceInfo>();
            
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    try
                    {
                        if (ni.OperationalStatus == OperationalStatus.Up && 
                            (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet || 
                             ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                        {
                            var info = new NetworkInterfaceInfo
                            {
                                Name = ni.Name ?? "Unknown",
                                Description = ni.Description ?? "Unknown",
                                InterfaceType = ni.NetworkInterfaceType.ToString(),
                                Status = ni.OperationalStatus.ToString(),
                                Speed = ni.Speed,
                                Id = ni.Id ?? "Unknown"
                            };

                            // Get IP properties
                            try
                            {
                                var ipProps = ni.GetIPProperties();
                                if (ipProps != null)
                                {
                                    var ipv4Props = ipProps.GetIPv4Properties();
                                    if (ipv4Props != null)
                                    {
                                        info.CurrentMTU = ipv4Props.Mtu;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"خطا در دریافت اطلاعات IP برای {ni.Name}: {ex.Message}");
                                info.CurrentMTU = -1;
                            }

                            interfaces.Add(info);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"خطا در پردازش رابط شبکه: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطا در دریافت رابط‌های شبکه: {ex.Message}");
            }

            return interfaces;
        }

        public static bool SetMTU(string interfaceName, int mtu)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = "netsh";
                process.StartInfo.Arguments = $"interface ipv4 set subinterface \"{interfaceName}\" mtu={mtu}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطا در تنظیم MTU: {ex.Message}");
            }
        }

        public static int GetCurrentMTU(string interfaceName)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = "netsh";
                process.StartInfo.Arguments = $"interface ipv4 show subinterfaces \"{interfaceName}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Parse the output to find MTU
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("MTU"))
                    {
                        var parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            if (int.TryParse(part, out int mtu))
                            {
                                return mtu;
                            }
                        }
                    }
                }

                return -1;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطا در دریافت MTU: {ex.Message}");
            }
        }

        public static string GetDefaultGateway()
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = "route";
                process.StartInfo.Arguments = "print 0.0.0.0";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("0.0.0.0") && line.Contains("0.0.0.0"))
                    {
                        var parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4)
                        {
                            return parts[3];
                        }
                    }
                }

                return "8.8.8.8"; // Default fallback
            }
            catch
            {
                return "8.8.8.8"; // Default fallback
            }
        }
    }

    public class NetworkInterfaceInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string InterfaceType { get; set; }
        public string Status { get; set; }
        public long Speed { get; set; }
        public string Id { get; set; }
        public int CurrentMTU { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Description}) - MTU: {CurrentMTU}";
        }
    }
}