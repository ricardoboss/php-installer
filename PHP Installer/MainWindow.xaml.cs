using NLog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using NLog.Targets;
using PHP_Installer.Converters;
using PHP_Installer.Properties;

namespace PHP_Installer
{
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static MainWindow()
        {
            // settings get reloaded at the end of this method (doesn't matter if they were upgraded or not)
            Settings.Default.Upgrade();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Inner Classes
        private class UpdateException : Exception
        {
            public UpdateException(string s) : base(s) {}
        }
        #endregion
        
        #region Properties

        private ObservableCollection<object> DownloadEntries { get; } = new ObservableCollection<object>();

        private volatile bool updating;
        public bool IsUpdating
        {
            get => updating || IsInstalling;
            set
            {
                updating = value;

                OnPropertyChanged();
            }
        }

        private volatile bool installing;
        public bool IsInstalling
        {
            get => installing;
            set
            {
                installing = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsUpdating));
            }
        }

        private string lastInstallationPath;
        private string LastInstallationPath
        {
            get => lastInstallationPath;
            set
            {
                lastInstallationPath = value;

                // FIXME: workaround, cant get it done with bindings
                TextBoxInstallationPath.Text = value;
                InfoInstallationPath.Text = InstallationPath ?? "?";

                OnPropertyChanged();
            }
        }

        private DownloadEntry selectedDownloadEntry;
        public DownloadEntry SelectedDownloadEntry
        {
            get => selectedDownloadEntry;
            set
            {
                selectedDownloadEntry = value;

                OnPropertyChanged();
            }
        }

        private string InstallationPath => SelectedDownloadEntry != null
            ? Path.Combine(LastInstallationPath, SelectedDownloadEntry.Foldername)
            : null;

        private int downloadProgress;
        public int DownloadProgress
        {
            get => downloadProgress;
            set
            {
                downloadProgress = value;

                OnPropertyChanged();
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            Application.Current.Exit += (sender, args) =>
            {
                // when the application exists, we want to store the used installation path for the next time
                Settings.Default.LastInstallationPath = LastInstallationPath;

                // don't forget to save!
                Settings.Default.Save();
            };

            // when the user selects an entry in the list
            ObjectToFrameworkElementConverter.SelectionChanged += entry =>
            {
                // store a reference to the entry
                SelectedDownloadEntry = entry;

                // update the info to where PHP will be installed
                InfoInstallationPath.Text = InstallationPath;
            };

            // load the last used installation path (default is C:\php)
            LastInstallationPath = Settings.Default.LastInstallationPath;

            // create binding
            // FIXME: creating this binding in xaml causes it not to get updates; need help
            ListBoxDownloadEntries.ItemsSource = DownloadEntries;

            // update the links list when the application starts
            Task.Factory.StartNew(UpdateLinks);
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

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // create a textblock to tell the user to report the exception
                    var item = new TextBlock
                    {
                        Inlines =
                        {
                            new Run(e.Message + "\r\n"),
                            new Run(" Please report this to "),
                            new Hyperlink(new Run("github.com/MCMainiac/php-installer/issues"))
                            {
                                NavigateUri = new Uri("https://github.com/MCMainiac/php-installer/issues")
                            },
                            new Run(" so we can fix it.\r\nAlso provide the contents of the log file at " + GetLogFileName())
                        }
                    };

                    DownloadEntries.Add(item);
                });
            }
            finally
            {
                IsUpdating = false;
            }
        }

        private static string GetLogFileName()
        {
            var fileTarget = LogManager.Configuration.FindTargetByName<FileTarget>("file");
            var info = new LogEventInfo { TimeStamp = DateTime.Now };

            return fileTarget.FileName.Render(info);
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

            Task.Factory.StartNew(() => Install(SelectedDownloadEntry, destPath));
        }

        private async void Install(DownloadEntry entry, string destPath)
        {
            // check if there is an installtion running
            if (IsInstalling)
            {
                MessageBox.Show("Installation already running!", "Existing installation", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            // starting installation...
            Logger.Info($"Starting installation of \"{entry}\" to path \"{destPath}\"");
            IsInstalling = true;

            try
            {
                // check if the destination path exists and delete it if it does
                if (Directory.Exists(destPath))
                {
                    Logger.Debug("Deleting existing destination directory...");

                    // delete the previous installation
                    Directory.Delete(destPath, true);
                }

                // create the destination directory
                Logger.Debug("Creating destination directory...");
                Directory.CreateDirectory(destPath);

                // the temp file exists in the destination folder
                var tempFilename = Path.Combine(destPath, "download.zip");

                Logger.Debug($"Using \"{tempFilename}\" as temporary file");

                // check if the temp file already exists
                if (File.Exists(tempFilename))
                {
                    Logger.Debug("Temporary file already exists! Deleting...");

                    File.Delete(tempFilename);
                }

                // download using WebClient
                using (var client = new WebClient())
                {
                    try
                    {
                        Logger.Debug($"Downloading file \"{entry.FullUrl}\"...");

                        // set a User-Agent since windows.php.net blocks empty user agents
                        var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
                        client.Headers[HttpRequestHeader.UserAgent] = $"PHP-Installer/{version} (github.com/MCMainiac/php-installer)";

                        // for updating the progress bar
                        client.DownloadProgressChanged += (sender, args) =>
                        {
                            if (args.ProgressPercentage % 20 == 0)
                                Logger.Info($"Download progress: {args.ProgressPercentage}% ({args.BytesReceived}/{args.TotalBytesToReceive})");

                            Application.Current.Dispatcher.Invoke(() => 
                                DownloadProgress = args.ProgressPercentage
                            );
                        };

                        // download the file
                        await client.DownloadFileTaskAsync(entry.FullUrl, tempFilename);

                        Logger.Debug("File downloaded!");
                    }
                    catch (WebException wex)
                    {
                        var hwr = wex.Response as HttpWebResponse;

                        // this is just for extended logging
                        if (!(hwr?.StatusCode.Equals(HttpStatusCode.OK) ?? true))
                            Logger.Error($"Received invalid status code: {hwr.StatusCode}. Are the used URLs still up-to-date?");

                        throw;
                    }
                }

                // unzip the downloaded file
                Logger.Debug("Unzipping file...");
                ZipFile.ExtractToDirectory(tempFilename, destPath);

                // check if these files exist
                var phpIniDevelopment = Path.Combine(destPath, "php.ini-development");
                var phpIniProduction = Path.Combine(destPath, "php.ini-production");

                // MAYBE: introduce more checks to test the decompression was successful
                if (!File.Exists(phpIniDevelopment) || !File.Exists(phpIniProduction))
                    throw new Exception("Some required files were not found!");

                // check if a php.ini file exists
                var phpIniFile = Path.Combine(destPath, "php.ini");
                if (!File.Exists(phpIniFile))
                {
                    // ask the user if they want to copy one of the default files
                    var iniDialog = new DefaultIniDialog();
                    var result = iniDialog.ShowDialog();

                    switch (result)
                    {
                        case DefaultIniDialog.ProductionResult:
                            File.Copy(phpIniProduction, phpIniFile);
                            break;
                        case DefaultIniDialog.DevelopmentResult:
                            File.Copy(phpIniDevelopment, phpIniFile);
                            break;
                    }

                    // MAYBE: ask user if they want to edit the php.ini
                }

                // start this application with additional arguments and elevated privileges to update the environment variable
                var psi = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = Assembly.GetExecutingAssembly().Location,
                    Verb = "runas",
                    Arguments = $"updatePath='{destPath}'"
                };

                // try to start the process and wait for its exit
                try
                {
                    Process.Start(psi)?.WaitForExit();
                }
                catch (Exception e)
                {
                    Logger.Warn(e, "Error while setting environment variable.");

                    MessageBox.Show(
                        $"Could not set environment variable! You have to add this to your PATH manually to be able to use PHP in your command line:\r\n\"{destPath}\"", "Could not update PATH", MessageBoxButton.OK);
                }

                // delete temp file; it is no longer of use
                File.Delete(tempFilename);

                Logger.Info("Installation successful");

                // installation finished
                MessageBox.Show($"Installation successful! You can now use your fresh PHP installation at \"{destPath}\"", "Installation completed", MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // reset download progress
                DownloadProgress = 0;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while installing.");

                MessageBox.Show($"An error occurred while installing the PHP environment: {e.Message}\r\nPlease submit your log file (located at \"{GetLogFileName()}\") to github.com/MCMainiac/php-installer/issues so that we can help you fix this issue.", "Error while installing", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsInstalling = false;
            }
        }

        private void HyperlinkAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutBox();
            aboutWindow.Show();
        }
    }
}
