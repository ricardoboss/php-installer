using System.Diagnostics;

namespace PHP_Installer.Models
{
    public class DownloadEntry : BaseModel
    {
        public const string BaseUri = "https://windows.php.net";
        public const string DownloadsUri = BaseUri + "/download/";

        private string uri;
        public string Uri {
            get => uri;
            set => SetProperty(ref uri, value, nameof(Uri), nameof(FullUrl));
        }

        private string version;
        public string Version {
            get => version;
            set => SetProperty(ref version, value, nameof(Version), nameof(Foldername));
        }

        private bool threadSafe;
        public bool ThreadSafe {
            get => threadSafe;
            set => SetProperty(ref threadSafe, value, nameof(ThreadSafe), nameof(Foldername));
        }

        private string compilerVersion;
        public string CompilerVersion {
            get => compilerVersion;
            set => SetProperty(ref compilerVersion, value);
        }

        private string architecture;
        public string Architecture {
            get => architecture;
            set => SetProperty(ref architecture, value);
        }

        private bool debugPack;
        public bool DebugPack {
            get => debugPack;
            set => SetProperty(ref debugPack, value, nameof(DebugPack), nameof(Foldername));
        }

        public string FullUrl => BaseUri + Uri;
        public string Foldername => Version + (ThreadSafe ? "" : "-nts") + (DebugPack ? "-debug" : "");

        public override string ToString()
        {
            return $"PHP {Version} {Architecture} {(ThreadSafe ? "" : "NTS ")}({(DebugPack ? "Debug Pack, " : "")}{CompilerVersion})";
        }
    }
}
