namespace TorrentStream {

    /// <summary>
    /// Global configuration.
    /// </summary>
    public static class GlobalConfiguration {

        public static int Port { get; set; } = 0;

        public static string BaseFolder { get; set; } = "";

        public static string? ListenAddress { get; set; } = "";

        public static bool ShowUI { get; set; } = false;

    }

}
