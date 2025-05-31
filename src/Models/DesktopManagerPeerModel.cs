namespace TorrentStream.Models {

    public record DesktopManagerPeerModel {

        public double Percent { get; init; }
        public string Address { get; init; } = "";
        public int Port { get; init; }
        public string Client { get; internal set; }
        public string DownloadSpeed { get; internal set; }
        public string UploadSpeed { get; internal set; }
    }

}