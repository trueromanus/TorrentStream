using System.Text.Json.Serialization;
using TorrentStream.Models;

namespace TorrentStream.SerializerContexts {

    [JsonSerializable ( typeof ( IEnumerable<FullManagerModel> ) )]
    [JsonSerializable ( typeof ( IEnumerable<TorrentFileModel> ) )]
    [JsonSerializable ( typeof ( TorrentFileModel ) )]
    [JsonSerializable ( typeof ( StatusModel ) )]
    [JsonSerializable ( typeof ( ManagerModel ) )]
    [JsonSerializable ( typeof ( IEnumerable<StatusModel> ) )]
    [JsonSerializable ( typeof ( IEnumerable<ManagerModel> ) )]
    [JsonSerializable ( typeof ( List<ManagerModel> ) )]
    [JsonSerializable ( typeof ( string ) )]
    [JsonSerializable ( typeof ( int ) )]
    public partial class TorrentStreamSerializerContext : JsonSerializerContext {
    }

}
