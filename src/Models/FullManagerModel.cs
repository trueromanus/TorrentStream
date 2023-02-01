namespace TorrentStream.Models {

    public record FullManagerModel {

        /// <summary>
        /// Path to torrent file.
        /// </summary>
        public string DownloadPath { get; init; } = "";

        /// <summary>
        /// Identifier.
        /// </summary>
        public int Identifier { get; init; }

        /// <summary>
        /// All downloaded.
        /// </summary>
        public bool AllDownloaded { get; init; }

        /// <summary>
        /// Files.
        /// </summary>
        public IEnumerable<TorrentFileModel> Files { get; init; } = Enumerable.Empty<TorrentFileModel>();

    }

}
