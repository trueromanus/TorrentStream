namespace TorrentStream.Models
{

    /// <summary>
    /// Status model.
    /// </summary>
    public class StatusModel
    {

        /// <summary>
        /// Path to torrent
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// Identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// All is ended.
        /// </summary>
        public bool All { get; set; }

        /// <summary>
        /// Status in files.
        /// </summary>
        public IDictionary<int, int> Files { get; set; } = new Dictionary<int, int>();

    }

}