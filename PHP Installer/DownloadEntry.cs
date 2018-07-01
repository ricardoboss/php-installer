namespace PHP_Installer
{
    public class DownloadEntry
    {
        public const string BaseUri = "https://windows.php.net";
        public const string DownloadsUri = BaseUri + "/download/";

        public string Uri { get; set; }
        public string Version { get; set; }
        public bool ThreadSafe { get; set; }
        public string CompilerVersion { get; set; }
        public string Architecture { get; set; }
        public bool DebugPack { get; set; }

        public string FullUrl => BaseUri + Uri;
        public string Foldername => Version + (ThreadSafe ? "" : "-nts") + (DebugPack ? "-debug" : "");

        public override string ToString()
        {
            return $"PHP {Version} {Architecture} {(ThreadSafe ? "" : "NTS ")}({(DebugPack ? "Debug Pack, " : "")}{CompilerVersion})";
        }
    }
}
