using MonoTorrent.Client;
using System.Text.Json.Serialization;

namespace TorrentStream.Models {

    /// <summary>
    /// Torrent manager model.
    /// </summary>
    public class ManagerModel {

        /// <summary>
        /// Path to torrent file.
        /// </summary>
        public string DownloadPath { get; set; } = "";

        /// <summary>
        /// Identifier.
        /// </summary>
        public int Identifier { get; set; }

        /// <summary>
        /// Manager.
        /// </summary>
        [JsonIgnore]
        public TorrentManager? Manager { get; set; }

        /// <summary>
        /// Metadata identifier.
        /// </summary>
        public string? MetadataId { get; set; }

    }

}
