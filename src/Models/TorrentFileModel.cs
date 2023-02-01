namespace TorrentStream.Models {

    /// <summary>
    /// Torrent file model.
    /// </summary>
    public record TorrentFileModel {

        public int PercentComplete { get; init; }

        public bool IsDownloaded { get; init; }

        public string DownloadedPath { get; init; } = "";

    }

}