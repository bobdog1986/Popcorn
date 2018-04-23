using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using NLog;

namespace Popcorn.Helpers
{
    public class ApplicationInsightsHelper
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        public static TelemetryClient TelemetryClient { get; private set; }

        public static string UserName;
        public static string Ip;
        public static string OperatingSystem;
        public static string Type;
        public static string SessionId;
        public static string UserAgent;
        public static string OemName;
        public static string Model;
        public static string Version;

        public static async Task Initialize()
        {
            try
            {
                TelemetryClient =
                    new TelemetryClient(TelemetryConfiguration.Active);
                UserName = Environment.UserName;
                OperatingSystem = Environment.OSVersion.ToString();
                Type = "PC";
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                Version = fvi.FileVersion;
                UserAgent = $"Popcorn/{fvi.FileVersion}";
                SessionId = Guid.NewGuid().ToString();
                foreach (var item in new System.Management.ManagementObjectSearcher(
                    "Select * from Win32_ComputerSystem").Get())
                {
                    var model = item["model"] as string;
                    if (!string.IsNullOrEmpty(model))
                    {
                        Model = model;
                    }

                    var manufacturer = item["manufacturer"] as string;
                    if (!string.IsNullOrEmpty(manufacturer))
                    {
                        OemName = manufacturer;
                    }
                }

                Task.Run(async () =>
                {
                    using (var client = new HttpClient())
                    {
                        var result = await client.GetStringAsync("http://checkip.dyndns.org/");
                        Ip = result.Split(new[] {':'}).Last().Trim().Split(new[] {'<'}).First().Trim();
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}