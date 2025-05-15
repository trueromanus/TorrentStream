namespace TorrentStream.Models {

    public record DesktopTorrentFileModel : TorrentFileModel {

        public double Percent { get; set; }

        public string Size { get; init; } = "";

        public string Name { get; set; } = "";

        public string Priority { get; set; } = "";

        public string Remaining { get; set; } = "";

    }

}
