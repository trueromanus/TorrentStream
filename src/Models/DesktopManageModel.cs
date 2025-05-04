namespace TorrentStream.Models {

    public record DesktopManageModel : FullManagerModel {

        public string TorrentName { get; init; } = "";

        public double Percent { get; set; }

        public string Size { get; init; } = "";

        public string Status { get; init; } = "";

        public int Peers { get; init; }
    }

}
