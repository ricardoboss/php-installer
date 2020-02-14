using NLog;
using NLog.Targets;
using PHP_Installer.Exceptions;
using PHP_Installer.Models;
using PHP_Installer.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PHP_Installer.ViewModels
{
    class InstallerViewModel : BaseViewModel<Installer>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public ObservableCollection<DownloadEntry> DownloadEntries { get; } = new ObservableCollection<DownloadEntry>();

        private bool updating;
        public bool IsUpdating
        {
            get => updating || Model.IsInstalling;
            set => SetProperty(ref updating, value);
        }


        private string lastInstallationPath;
        private string LastInstallationPath
        {
            get => lastInstallationPath;
            set => SetProperty(ref lastInstallationPath, value);
        }

        private DownloadEntry selectedDownloadEntry;
        public DownloadEntry SelectedDownloadEntry
        {
            get => selectedDownloadEntry;
            set => SetProperty(ref selectedDownloadEntry, value);
        }

        private string InstallationPath => SelectedDownloadEntry != null
            ? Path.Combine(LastInstallationPath, SelectedDownloadEntry.Foldername)
            : null;

        private int downloadProgress;
        public int DownloadProgress
        {
            get => downloadProgress;
            set => SetProperty(ref downloadProgress, value);
        }

        public InstallerViewModel() :
            base(new Installer())
        {
            Application.Current.Exit += (sender, args) =>
            {
                // when the application exists, we want to store the used installation path for the next time
                Settings.Default.LastInstallationPath = LastInstallationPath;

                // don't forget to save!
                Settings.Default.Save();
            };

            // load the last used installation path (default is C:\php)
            LastInstallationPath = Settings.Default.LastInstallationPath;

            // update the links list when the application starts
            Task.Factory.StartNew(UpdateLinks);
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(UpdateLinks);
        }

        private void ButtonBrowseInstallPath_Click(object sender, RoutedEventArgs e)
        {
            // show a dialog in which the user selects a custom install location
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = LastInstallationPath,
                Description = "Select a folder where your PHP environment will be set up in",
                ShowNewFolderButton = true
            })
            {
                var result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    LastInstallationPath = fbd.SelectedPath;
            }
        }

        private void ButtonInstall_Click(object sender, RoutedEventArgs e)
        {
            // check if there is an installtion running
            if (Model.IsInstalling)
            {
                MessageBox.Show("Installation already running!", "Existing installation", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            // determine installation path
            var destPath = InstallationPath;

            // check if the destination path already exists
            if (Directory.Exists(destPath))
            {
                // ask use if they want to overwrite the existing installation
                var result = MessageBox.Show(
                    "Installation path already exists! The application will remove every existing files proir to installation. Do you want to proceed anyway?",
                    "Existing installation", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

                if (result == MessageBoxResult.No)
                {
                    MessageBox.Show("The installation was cancelled due to an already existing installation.",
                        "Installation cancelled", MessageBoxButton.OK);

                    return;
                }
            }

            Task.Factory.StartNew(() => Model.Install(SelectedDownloadEntry, destPath));
        }

        private void UpdateLinks()
        {
            // if already updating -> skip
            if (IsUpdating)
                return;

            Logger.Debug("Updating links");
            IsUpdating = true;

            try
            {
                // download/request the html page with the download links
                string page;
                using (var client = new WebClient())
                {
                    page = client.DownloadString(new Uri(DownloadEntry.DownloadsUri));
                }

                // check if data has been received
                if (string.IsNullOrEmpty(page.Trim()))
                    throw new UpdateException("No data received.");

                // run this regex pattern over the html page to filter out links to .zip-archives (with additional info from the filename)
                var matches = Regex.Matches(page, "\"(.+?php-(debug-pack)?-?([\\d\\.]+)-(nts)?-?Win32-(\\w+)-(\\w+)\\.zip)");

                // check if links were found
                if (matches.Count == 0)
                    throw new UpdateException("No links matched.");

                // clear old entries
                Application.Current.Dispatcher.Invoke(() => DownloadEntries.Clear());

                // add each match as a new entry
                foreach (Match match in matches)
                {
                    // must be added by the application dispatcher since it created the list object
                    Application.Current.Dispatcher.Invoke(() => DownloadEntries.Add(new DownloadEntry
                    {
                        Uri = match.Groups[1].Value,
                        DebugPack = match.Groups[2].Success,
                        Version = match.Groups[3].Value,
                        ThreadSafe = match.Groups[4].Success,
                        CompilerVersion = match.Groups[5].Value,
                        Architecture = match.Groups[6].Value
                    }));
                }

                Logger.Debug("Links successfully updated.");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while updating links.");

                //Application.Current.Dispatcher.Invoke(() =>
                //{
                //    // create a textblock to tell the user to report the exception
                //    var item = new TextBlock
                //    {
                //        Inlines =
                //        {
                //            new Run(e.Message + "\r\n"),
                //            new Run(" Please report this to "),
                //            new Hyperlink(new Run("github.com/MCMainiac/php-installer/issues"))
                //            {
                //                NavigateUri = new Uri("https://github.com/MCMainiac/php-installer/issues")
                //            },
                //            new Run(" so we can fix it.\r\nAlso provide the contents of the log file at " + GetLogFileName())
                //        }
                //    };

                //    DownloadEntries.Add(item);
                //});
            }
            finally
            {
                IsUpdating = false;
            }
        }
    }
}
