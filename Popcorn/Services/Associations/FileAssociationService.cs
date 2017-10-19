using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using NLog;

namespace Popcorn.Services.Associations
{
    public class FileAssociationService : IFileAssociationService
    {
        /// <summary>
        /// Logger of the class
        /// </summary>
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        // needed so that Explorer windows get refreshed after the registry is updated
        [System.Runtime.InteropServices.DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        private const int SHCNE_ASSOCCHANGED = 0x8000000;
        private const int SHCNF_FLUSH = 0x1000;
        private static string _filePath = Process.GetCurrentProcess().MainModule.FileName;

        private FileAssociation _association = new FileAssociation
        {
            Extension = ".torrent",
            ProgId = "Popcorn",
            FileTypeDescription = "Torrent File",
            ExecutableFilePath = _filePath
        };

        public void RegisterTorrentFileAssociation()
        {
            try
            {
                RegisterTorrentFileAssociation(_association);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void RegisterMagnetLinkAssociation()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\magnet"))
                {
                    var filePath = Process.GetCurrentProcess().MainModule.FileName;

                    key.SetValue("", "URL:Popcorn Magnet");
                    key.SetValue("URL Protocol", "");

                    using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                    {
                        defaultIcon.SetValue("", filePath + ",1");
                    }

                    using (var commandKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        commandKey.SetValue("", "\"" + filePath + "\" \"%1\"");
                    }
                }

                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void UnregisterMagnetLinkAssociation()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree("SOFTWARE\\Classes\\magnet", false);
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void UnregisterTorrentFileAssociation()
        {
            try
            {
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + _association.ProgId, false);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public bool TorrentFileAssociationIsEnabled()
        {
            return CheckIfAssociationIsSet(_association.Extension,
                _association.ProgId,
                _association.FileTypeDescription,
                _association.ExecutableFilePath);
        }

        public bool MagneLinkAssociationIsEnabled()
        {
            try
            {
                if (Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\magnet") == null)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        private void RegisterTorrentFileAssociation(params FileAssociation[] associations)
        {
            try
            {
                bool madeChanges = false;
                foreach (var association in associations)
                {
                    madeChanges |= SetAssociation(
                        association.Extension,
                        association.ProgId,
                        association.FileTypeDescription,
                        association.ExecutableFilePath);
                }

                if (madeChanges)
                {
                    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private bool CheckIfAssociationIsSet(string extension, string progId, string fileTypeDescription,
            string applicationFilePath)
        {
            try
            {
                return CheckIfValuePresent(@"Software\Classes\" + extension, progId) &&
                       CheckIfValuePresent(@"Software\Classes\" + progId, fileTypeDescription) && CheckIfValuePresent(
                           $@"Software\Classes\{progId}\shell\open\command",
                           "\"" + applicationFilePath + "\" \"%1\"");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        private bool SetAssociation(string extension, string progId, string fileTypeDescription,
            string applicationFilePath)
        {
            try
            {
                bool madeChanges = false;
                madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + extension, progId);
                madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + progId, fileTypeDescription);
                madeChanges |= SetKeyDefaultValue($@"Software\Classes\{progId}\shell\open\command",
                    "\"" + applicationFilePath + "\" \"%1\"");
                return madeChanges;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        private bool SetKeyDefaultValue(string keyPath, string value)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    if (key.GetValue(null) as string != value)
                    {
                        key.SetValue(null, value);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        private bool CheckIfValuePresent(string keyPath, string value)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    if (key == null) return false;
                    if (key.GetValue(null) as string == value)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }
    }
}