namespace TorrentStream.Models {

    public record DesktopManagerPeerModel {
        public string Identifier { get; set; } = "";
        public double Percent { get; init; }
        public string Address { get; init; } = "";
        public int Port { get; init; }
        public string Client { get; init; } = "";
        public string DownloadSpeed { get; init; } = "";
        public string UploadSpeed { get; init; } = "";
    }

}