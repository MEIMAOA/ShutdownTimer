using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace ShutdownTimer.Services
{
    public class AdapterInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Type { get; set; } = "";
        public string Status { get; set; } = "";
        public string Speed { get; set; } = "";
        public string MacAddress { get; set; } = "";
    }

    public class PingResult
    {
        public string Host { get; set; } = "";
        public bool Success { get; set; }
        public long RoundtripMs { get; set; }
        public string Message { get; set; } = "";
    }

    public static class NetworkService
    {
        public static List<AdapterInfo> GetAdapters()
        {
            var list = new List<AdapterInfo>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    string speed = ni.Speed > 0
                        ? $"{ni.Speed / 1_000_000} Mbps"
                        : "-";

                    list.Add(new AdapterInfo
                    {
                        Name = ni.Name,
                        Description = ni.Description,
                        Type = ni.NetworkInterfaceType.ToString(),
                        Status = ni.OperationalStatus.ToString(),
                        Speed = speed,
                        MacAddress = ni.GetPhysicalAddress()?.ToString() ?? ""
                    });
                }
            }
            catch { }
            return list;
        }

        public static async Task<PingResult> PingAsync(string host, int timeoutMs = 3000)
        {
            var result = new PingResult { Host = host };
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, timeoutMs);
                result.Success = reply.Status == IPStatus.Success;
                result.RoundtripMs = reply.RoundtripTime;
                result.Message = result.Success
                    ? $"延迟 {reply.RoundtripTime} ms"
                    : reply.Status.ToString();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>执行网络诊断：ping 网关、114.114.114.114、8.8.8.8</summary>
        public static async Task<List<PingResult>> RunDiagnosticsAsync()
        {
            var results = new List<PingResult>();

            results.Add(await PingAsync("114.114.114.114"));
            results.Add(await PingAsync("8.8.8.8"));
            results.Add(await PingAsync("www.baidu.com"));

            return results;
        }
    }
}