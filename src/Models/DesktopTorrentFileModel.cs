namespace TorrentStream.Models {

    public record DesktopTorrentFileModel : TorrentFileModel {

        public string Identifier { get; set; } = "";

        public double Percent { get; set; }

        public string Size { get; init; } = "";

        public string Name { get; set; } = "";

        public string Priority { get; set; } = "";

        public string Remaining { get; set; } = "";

    }

}
