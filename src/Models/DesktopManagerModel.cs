﻿namespace TorrentStream.Models {

    public record DesktopManagerModel : FullManagerModel {

        public string TorrentName { get; init; } = "";

        public double Percent { get; set; }

        public string Size { get; init; } = "";

        public string Status { get; init; } = "";

        public int Peers { get; init; }

        public int Seeds { get; init; }

        public string DownloadSpeed { get; init; } = "";

        public string UploadSpeed { get; init; } = "";

        public string Unique => DesktopUI.ComputeSha256Hash ( TorrentName + Size );

        public new IEnumerable<DesktopTorrentFileModel> Files { get; init; } = Enumerable.Empty<DesktopTorrentFileModel> ();

        public IEnumerable<DesktopManagerPeerModel> TorrentPeers { get; init; } = Enumerable.Empty<DesktopManagerPeerModel> ();

    }

}
