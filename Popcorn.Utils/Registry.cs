using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Popcorn.Utils
{
    public class Registry
    {
        public static string GetMachineGuid()
        {
            var registryValue = string.Empty;
            using (var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32))
            {
                try
                {
                    using (var innerKey = localKey.OpenSubKey(@"SOFTWARE\\Microsoft\\Cryptography"))
                    {
                        registryValue = innerKey.GetValue("MachineGuid").ToString();
                    }
                }
                catch (NullReferenceException)
                {

                }

                return registryValue;
            }
        }
    }
}
