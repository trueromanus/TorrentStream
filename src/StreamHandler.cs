using MonoTorrent.Client;
using MonoTorrent;
using System.Collections.Concurrent;
using MonoTorrent.Streaming;
using System.Runtime.InteropServices;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TorrentStream.Models;

namespace TorrentStream {

    public static class TorrentHandler {

        private static ClientEngine m_ClientEngine;

        static TorrentHandler () => m_ClientEngine = new ClientEngine ();

        private static readonly string DownloadsPath = Path.Combine ( Path.GetDirectoryName ( AppContext.BaseDirectory ) ?? "", "Downloads" );

        private static readonly string StateFilePath = Path.Combine ( Path.GetDirectoryName ( AppContext.BaseDirectory ) ?? "", "statefile" );

        private static readonly string InnerStateFilePath = Path.Combine ( Path.GetDirectoryName ( AppContext.BaseDirectory ) ?? "", "innerstatefile" );

        public static readonly HashSet<string> m_DownloadedTorrents = new ();

        public static ConcurrentDictionary<string, ManagerModel> m_TorrentManagers = new ();

        public static readonly ConcurrentDictionary<string, IHttpStream> m_TorrentStreams = new ();

        public static readonly ConcurrentDictionary<WebSocket, bool> m_ActiveWebSockets = new ();

        private static async Task<(Stream?, bool)> GetTorrentStream ( string url ) {
            if ( m_DownloadedTorrents.Contains ( url ) ) return (null, true);

            try {
                var httpClient = new HttpClient ();
                var downloadedContent = await httpClient.GetStreamAsync ( url );
                var innerStream = new MemoryStream ();
                await downloadedContent.CopyToAsync ( innerStream );
                m_DownloadedTorrents.Add ( url );
                return (innerStream, true);
            } catch {
                return (null, false);
            }
        }

        private static string GetStringValueFromQuery ( string key, HttpContext httpContext ) {
            return httpContext.Request.Query.ContainsKey ( key ) ? httpContext.Request.Query.Where ( a => a.Key == key ).FirstOrDefault ().Value.First () ?? "" : "";
        }

        public static async Task StartDownloadForOnlineStreaming ( HttpContext context ) {
            context.Response.ContentType = "text/plain";
            if ( context.Request.Query.Count != 3 ) {
                context.Response.StatusCode = 204;
                return;
            }

            var fileIndex = GetStringValueFromQuery ( "index", context );
            var torrentPath = GetStringValueFromQuery ( "path", context );
            var identifier = GetStringValueFromQuery ( "id", context );

            var activeFileIndex = Convert.ToInt32 ( fileIndex );

            var (torrentStream, result) = await GetTorrentStream ( torrentPath );

            if ( !result ) {
                context.Response.StatusCode = 400;
                return;
            }

            try {
                var manager = await GetManager ( torrentPath, torrentStream, identifier );
                if ( manager == null ) {
                    context.Response.StatusCode = 404;
                    return;
                }
                var iterator = 0;
                var torrentFiles = manager.Files
                    .OrderBy ( a => a.Path )
                    .ToList();
                foreach ( var file in torrentFiles ) {
                    await manager.SetFilePriorityAsync ( file, iterator == activeFileIndex ? Priority.High : Priority.DoNotDownload );
                    iterator++;
                }

                if ( m_TorrentStreams.ContainsKey ( torrentPath ) ) {
                    if ( m_TorrentStreams.TryRemove ( torrentPath, out var stream ) ) stream.Dispose ();
                }

                var currentFile = torrentFiles.ElementAt(activeFileIndex);
                var isDownloaded = currentFile.BitField.PercentComplete >= 100;

                if ( !isDownloaded ) {
                    if ( manager.StreamProvider != null ) {
                        var httpStream = await manager.StreamProvider.CreateHttpStreamAsync ( currentFile, false );
                        if ( httpStream != null ) {
                            m_TorrentStreams.TryAdd ( torrentPath, httpStream );

                            context.Response.StatusCode = 302;
                            context.Response.Headers.Location = httpStream.FullUri;
                        }
                    }
                } else {
                    context.Response.StatusCode = 302;
                    context.Response.Headers.Location = ( RuntimeInformation.IsOSPlatform ( OSPlatform.Windows ) ? "file:///" : "file://" ) + currentFile.FullPath;
                }
            } catch {
                context.Response.StatusCode = 500;
            }
        }

        private static async Task<TorrentManager?> GetManager ( string torrentPath, Stream? torrentStream, string identifier ) {
            TorrentManager manager;
            if ( m_TorrentManagers.TryGetValue ( torrentPath, out var createdManager ) ) {
                manager = createdManager.Manager ?? throw new Exception ( "Manager is null!" );
            } else {
                if ( torrentStream == null ) return null;

                torrentStream.Position = 0;
                var torrent = await Torrent.LoadAsync ( torrentStream );
                manager = await m_ClientEngine.AddStreamingAsync ( torrent, DownloadsPath );
                await manager.StartAsync ();
                await manager.WaitForMetadataAsync ();
                m_TorrentManagers.TryAdd (
                    torrentPath,
                    new ManagerModel {
                        DownloadPath = torrentPath,
                        Manager = manager,
                        Identifier = Convert.ToInt32 ( identifier ),
                        MetadataId = manager.MetadataPath
                    }
                );
                await SaveState ();
            }

            return manager;
        }

        public static async Task StartFullDownload ( HttpContext context ) {
            context.Response.ContentType = "text/plain";
            if ( context.Request.Query.Count != 2 ) {
                context.Response.StatusCode = 204;
                return;
            }

            var torrentPath = GetStringValueFromQuery ( "path", context );
            var identifier = GetStringValueFromQuery ( "id", context );

            var (torrentStream, result) = await GetTorrentStream ( torrentPath );

            if ( !result ) {
                context.Response.StatusCode = 400;
                return;
            }

            try {
                TorrentManager manager;

                if ( m_TorrentManagers.TryGetValue ( torrentPath, out var createdManager ) ) {
                    manager = createdManager.Manager ?? throw new Exception ( "Manager is null!" );
                    foreach ( var file in manager.Files.Where ( a => a.Priority == Priority.DoNotDownload ) ) {
                        await manager.SetFilePriorityAsync ( file, Priority.Normal );
                    }
                } else {
                    if ( torrentStream == null ) {
                        context.Response.StatusCode = 404;
                        return;
                    }
                    torrentStream.Position = 0;
                    var torrent = await Torrent.LoadAsync ( torrentStream );
                    manager = await m_ClientEngine.AddStreamingAsync ( torrent, DownloadsPath );
                    await manager.StartAsync ();
                    manager.TorrentStateChanged += ManagerTorrentStateChanged;
                    m_TorrentManagers.TryAdd (
                        torrentPath,
                        new ManagerModel {
                            DownloadPath = torrentPath,
                            Manager = manager,
                            Identifier = Convert.ToInt32 ( identifier ),
                            MetadataId = manager.MetadataPath
                        }
                    );
                    SendMessageToSocket ( "nt:" + identifier );
                    await SaveState ();
                }
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync ( "Downloading started" );
            } catch {
                context.Response.StatusCode = 500;
            }
        }

        private static void ManagerTorrentStateChanged ( object? sender, TorrentStateChangedEventArgs e ) {
            if ( e.TorrentManager == null ) return;

            if ( e.TorrentManager.State == TorrentState.Seeding ) {
                var message = GetDownloadStatus ( e.TorrentManager );
                if ( !string.IsNullOrEmpty ( message ) ) SendMessageToSocket ( message );
            }

            Task.Run ( SaveState );
        }

        private static string GetDownloadStatus ( TorrentManager manager ) {
            var managerModel = m_TorrentManagers.Values.FirstOrDefault ( a => a.Manager == manager );
            if ( managerModel == null ) return "";

            if ( manager.Files.All ( a => a.BitField.AllTrue ) ) {
                var model = new StatusModel { Path = managerModel.DownloadPath, All = true, Id = managerModel.Identifier };
                return "ds:" + JsonSerializer.Serialize ( model );
            }

            return "";
        }

        private static void SendMessageToSocket ( string message ) {
            if ( !m_ActiveWebSockets.Any () ) return;

            var messageInBytes = new ReadOnlyMemory<byte> ( Encoding.UTF8.GetBytes ( message ) );
            foreach ( var socket in m_ActiveWebSockets.Keys ) {
                if ( socket.State != WebSocketState.Open ) continue;
                Task.Run ( async () => await socket.SendAsync ( messageInBytes, WebSocketMessageType.Text, true, CancellationToken.None ) );
            }
        }

        public static async Task Finalization ( HttpContext context ) {
            if ( context is null ) throw new ArgumentNullException ( nameof ( context ) );

            await m_ClientEngine.StopAllAsync ();
            foreach ( var manager in m_TorrentManagers ) {
                if ( manager.Value.Manager == null ) continue;

                await m_ClientEngine.RemoveAsync ( manager.Value.Manager );
            }
            m_TorrentManagers.Clear ();
            m_TorrentStreams.Clear ();
            m_DownloadedTorrents.Clear ();

            await SaveState ();

            if ( Directory.Exists ( DownloadsPath ) ) Directory.Delete ( DownloadsPath, true );

            SendMessageToSocket ( "dt:" );

            context.Response.StatusCode = 200;
            await context.Response.WriteAsync ( "Completed" );
        }

        public static async Task SaveStateAndStop () {
            await SaveState ();

            // close currently actived web sockets
            if ( m_ActiveWebSockets.Any () ) {
                foreach ( var socket in m_ActiveWebSockets.Keys ) {
                    await socket.CloseAsync ( WebSocketCloseStatus.NormalClosure, "server is down", CancellationToken.None );
                }
            }
        }

        public static async Task SaveState () {
            await m_ClientEngine.SaveStateAsync ( StateFilePath );
            await File.WriteAllTextAsync ( InnerStateFilePath, JsonSerializer.Serialize ( m_TorrentManagers.Values ) );
        }

        public static async Task LoadState () {
            if ( !File.Exists ( StateFilePath ) ) return;

            m_ClientEngine = await ClientEngine.RestoreStateAsync ( StateFilePath );
            await m_ClientEngine.StartAllAsync (); // immediate start downloading after restoring

            var content = await File.ReadAllTextAsync ( InnerStateFilePath );
            var torrentManagers = JsonSerializer.Deserialize<List<ManagerModel>> ( content );
            if ( torrentManagers == null ) return;
            foreach ( var torrentManager in torrentManagers ) {
                torrentManager.Manager = m_ClientEngine.Torrents.FirstOrDefault ( a => a.MetadataPath == torrentManager.MetadataId );
                if ( torrentManager.Manager != null ) torrentManager.Manager.TorrentStateChanged += ManagerTorrentStateChanged;
                m_TorrentManagers.TryAdd ( torrentManager.DownloadPath, torrentManager );
            }
        }

        public static async Task TorrentWebSocket ( HttpContext context ) {
            if ( context.WebSockets.IsWebSocketRequest ) {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync ();
                m_ActiveWebSockets.TryAdd ( webSocket, true );
                await StartSocketSession ( webSocket );
                if ( !m_ActiveWebSockets.TryRemove ( webSocket, out var result ) ) m_ActiveWebSockets.TryRemove ( webSocket, out var _ );
                return;
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }

        private static async Task StartSocketSession ( WebSocket webSocket ) {
            var buffer = new byte[1024].AsMemory ();

            while ( true ) {
                if ( webSocket.State != WebSocketState.Open ) break;

                var receiveResult = await webSocket.ReceiveAsync ( buffer, CancellationToken.None );

                if ( receiveResult.Count == 0 ) break;

                var messageContent = Encoding.UTF8.GetString ( buffer.ToArray () );
                var parts = messageContent.Split ( ":" );
                if ( parts.Length != 2 ) continue;

                switch ( parts[0] ) {
                    case "ds": //download status
                        if ( m_TorrentManagers.Any () ) await webSocket.SendAsync ( GetDownloadStatus (), WebSocketMessageType.Text, true, CancellationToken.None );
                        break;
                }
            }

            if ( m_ActiveWebSockets.ContainsKey ( webSocket ) ) m_ActiveWebSockets.TryRemove ( webSocket, out var _ );
        }

        private static ReadOnlyMemory<byte> GetDownloadStatus () {
            var keys = m_TorrentManagers.Keys;
            var result = new List<StatusModel> ();

            foreach ( var managerKey in keys ) {
                if ( m_TorrentManagers.TryGetValue ( managerKey, out var managerModel ) ) {
                    if ( managerModel.Manager == null ) continue;

                    var manager = managerModel.Manager;
                    if ( manager.Files.All ( a => a.BitField.AllTrue ) ) {
                        result.Add ( new StatusModel { Path = managerKey, All = true, Id = managerModel.Identifier } );
                        continue;
                    }
                    var model = new StatusModel { Path = managerKey, All = false, Id = managerModel.Identifier };
                    var index = 0;
                    foreach ( var file in manager.Files ) {
                        model.Files.Add ( index, Convert.ToInt32 ( file.BitField.PercentComplete ) );
                        index++;
                    }
                    result.Add ( model );
                }
            }

            return Encoding.UTF8.GetBytes ( "ds:" + JsonSerializer.Serialize ( result ) ).AsMemory ();
        }

        public static async Task GetTorrents ( HttpContext context ) {
            if ( m_TorrentManagers.IsEmpty ) {
                await context.Response.WriteAsync ( "[]" );
                return;
            }

            var managers = m_TorrentManagers.Values;

            var result = new List<FullManagerModel> ();

            foreach ( var manager in managers ) {
                if ( manager.Manager == null ) continue;

                result.Add (
                    new FullManagerModel {
                        Identifier = manager.Identifier,
                        DownloadPath = manager.DownloadPath,
                        AllDownloaded = manager.Manager.Bitfield.PercentComplete >= 100,
                        Files = manager.Manager.Files
                            .Select (
                                a => new TorrentFileModel {
                                    IsDownloaded = a.BitField.PercentComplete >= 100,
                                    PercentComplete = Convert.ToInt32 ( a.BitField.PercentComplete ),
                                    DownloadedPath = a.DownloadCompleteFullPath
                                }
                            )
                            .OrderBy ( a => a.DownloadedPath )
                            .ToList ()
                    }
                );
            }

            await context.Response.WriteAsJsonAsync ( result );
        }

        public static async Task ClearOnlyTorrent ( HttpContext context ) {
            context.Response.ContentType = "text/plain";
            if ( context.Request.Query.Count != 1 ) {
                context.Response.StatusCode = 204;
                return;
            }

            var downloadPath = GetStringValueFromQuery ( "path", context );

            if ( !m_TorrentManagers.ContainsKey ( downloadPath ) ) {
                await context.Response.WriteAsync ( "Already not exists" );
                return;
            }

            await RemoveTorrentFromTracker ( downloadPath );

            await SaveState ();

            SendMessageToSocket ( "dt:" );

            await context.Response.WriteAsync ( "Completed" );
        }

        private static async Task RemoveTorrentFromTracker ( string downloadPath ) {
            if ( m_TorrentManagers.TryGetValue ( downloadPath, out var torrentManager ) ) {
                if ( torrentManager.Manager != null ) {
                    await torrentManager.Manager.StopAsync ();
                    await m_ClientEngine.RemoveAsync ( torrentManager.Manager );

                    if ( !m_TorrentManagers.TryRemove ( downloadPath, out var _ ) ) {
                        m_TorrentManagers.TryRemove ( downloadPath, out var _ );
                    }
                }
            }

            m_DownloadedTorrents.Remove ( downloadPath );
            if ( !m_TorrentStreams.TryRemove ( downloadPath, out var _ ) ) m_TorrentStreams.TryRemove ( downloadPath, out var _ );
        }

        public static async Task ClearTorrentAndData ( HttpContext context ) {
            context.Response.ContentType = "text/plain";
            if ( context.Request.Query.Count != 1 ) {
                context.Response.StatusCode = 204;
                return;
            }

            var downloadPath = GetStringValueFromQuery ( "path", context );


            if ( m_TorrentManagers.TryGetValue ( downloadPath, out var torrent ) ) {
                if ( torrent == null || torrent.Manager == null ) {
                    await context.Response.WriteAsync ( "Already not exists" );
                    return;
                }
                await RemoveTorrentFromTracker ( downloadPath );
                Directory.Delete ( torrent.Manager.ContainingDirectory, true );
            }

            await SaveState ();

            SendMessageToSocket ( "dt:" );

            await context.Response.WriteAsync ( "Completed" );
        }

    }

}
