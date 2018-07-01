using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Threading;
using NLog;
using MessageBox = System.Windows.MessageBox;

namespace PHP_Installer
{
    public partial class App
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private void NavigateInBrowser(object sender, RoutedEventArgs routedEventArgs)
        {
            if (sender is Hyperlink hl && hl.NavigateUri != null)
            {
                Logger.Info("Navigating to " + hl.NavigateUri);

                Process.Start(hl.NavigateUri.ToString());
            }

            routedEventArgs.Handled = true;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            // global exception handlers
            Current.DispatcherUnhandledException += (o, args) => HandleException(args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (o, args) => HandleException(args.ExceptionObject as Exception);
            Dispatcher.UnhandledException += (o, args) => HandleException(args.Exception);
            TaskScheduler.UnobservedTaskException += (o, args) => HandleException(args.Exception);

            try
            {
                if (e.Args.Length > 0)
                    HandleCommandLine(e.Args);

                var mw = new MainWindow();
                mw.Show();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred while starting the application!");
            }
        }

        private static void HandleException(Exception e)
        {
            Logger.Error(e, "Unhandled exception!");

            MessageBox.Show("An unhandled exception occurred! Please report this at github.com/MCMainiac/php-installer", "Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static void HandleCommandLine(IEnumerable<string> args)
        {
            foreach (var arg in args)
            {
                var key = arg.Substring(0, arg.IndexOf("="));
                var value = arg.Substring(arg.IndexOf("=") + 1).Trim('"', '\'');

                switch (key)
                {
                    case "updatePath":
                        UpdatePath(value);
                        break;

                    default:
                        Logger.Error("Undefined flag: " + key);
                        
                        Environment.Exit(-1);
                        return;
                }
            }

            Environment.Exit(0);
        }

        private static bool IsElevated =>
            WindowsIdentity.GetCurrent().Owner?.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid) ?? false;

        private static void UpdatePath(string path)
        {
            if (!IsElevated)
            {
                Logger.Error("This application needs administrative rights to update the PATH environment variable!");

                MessageBox.Show("Administrative rights are required to perform the requested action.", "Administrative rights required", MessageBoxButton.OK, MessageBoxImage.Error);

                Environment.Exit(-1);
            }

            Logger.Info("Updating PATH environment variable");

            var originalPath =
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);

            if (originalPath == null)
            {
                Logger.Error("Unable to get current PATH environment variable.");

                Environment.Exit(-1);
            }

            if (!Directory.Exists(path))
            {
                Logger.Error("Directory does not exist: " + path);

                Environment.Exit(-1);
            }

            var parent = Directory.GetParent(path);

            // get original entries
            var originalPathEntries = originalPath.Split(';');

            // filter entries which are believed to be previous versions
            var removedEntries = originalPathEntries.Where(entry =>
            {
                entry = entry.TrimEnd('\\');

                if (string.IsNullOrEmpty(entry))
                    return false;

                var entryParent = Directory.GetParent(entry);

                return entryParent.FullName.Equals(parent.FullName);
            }).ToList();

            // default: do not remove old entries
            var result = MessageBoxResult.No;
            if (removedEntries.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append("Do you want to remove the following entries:\r\n");
                foreach (var entry in removedEntries)
                    sb.Append("- ").Append(entry.Length == 0 ? "<empty>" : entry).AppendLine();

                result = MessageBox.Show(sb.ToString(), "Old entries found", MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question, MessageBoxResult.Cancel);
            }

            var newPathEntries = new List<string>();
            switch (result)
            {
                // yes, remove old entries
                case MessageBoxResult.Yes:
                    newPathEntries.AddRange(originalPathEntries.Except(removedEntries));
                    break;

                // no, don't remove old entries
                case MessageBoxResult.No:
                    newPathEntries.AddRange(originalPathEntries);
                    break;

                // cancel, abort operation
                default:
                    Logger.Info("Operation cancelled.");

                    Environment.Exit(0);
                    break;
            }

            // add the new entry
            newPathEntries.Add(path);

            // join the entries
            var newPath = string.Join(";", newPathEntries);

            // update the path variable
            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Machine);
        }
    }
}
