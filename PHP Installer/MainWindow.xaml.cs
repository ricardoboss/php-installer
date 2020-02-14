using System.Windows;
using PHP_Installer.Properties;

namespace PHP_Installer
{
    public sealed partial class MainWindow
    {
        static MainWindow()
        {
            // settings get reloaded at the end of this method (doesn't matter if they were upgraded or not)
            Settings.Default.Upgrade();
        }

        public MainWindow()
        {
            InitializeComponent();


        }
    }
}
