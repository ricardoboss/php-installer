using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;

namespace PHP_Installer.Models
{
    public class Installer : BaseModel
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private bool installing;
        public bool IsInstalling
        {
            get => installing;
            set => SetProperty(ref installing, value);
        }

        public async void Install(DownloadEntry entry, string destPath)
        {
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

                            //Application.Current.Dispatcher.Invoke(() =>
                            //    DownloadProgress = args.ProgressPercentage
                            //);
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
                //DownloadProgress = 0;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while installing.");

                MessageBox.Show($"An error occurred while installing the PHP environment: {e.Message}\r\nPlease submit your log file (located at \"{App.GetLogFileName()}\") to github.com/MCMainiac/php-installer/issues so that we can help you fix this issue.", "Error while installing", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsInstalling = false;
            }
        }
    }
}
