using MonoTorrent.Client;
using MonoTorrent;
using System.Collections.Concurrent;
using MonoTorrent.Streaming;
using System.Runtime.InteropServices;

namespace TorrentStream {

    public static class TorrentHandler {

        public static readonly ClientEngine ClientEngine;

        static TorrentHandler () => ClientEngine = new ClientEngine ();

        private static readonly string DownloadsPath = Path.Combine ( Path.GetDirectoryName ( AppContext.BaseDirectory ) ?? "", "Downloads" );

        public static readonly HashSet<string> m_DownloadedTorrents = new ();

        public static readonly ConcurrentDictionary<string, TorrentManager> m_TorrentManagers = new ();

        public static readonly ConcurrentDictionary<string, IUriStream> m_TorrentStreams = new ();

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
            if ( context.Request.Query.Count != 2 ) {
                context.Response.StatusCode = 204;
                return;
            }

            var fileIndex = GetStringValueFromQuery ( "index", context );
            var torrentPath = GetStringValueFromQuery ( "path", context );

            var activeFileIndex = Convert.ToInt32 ( fileIndex );

            var (torrentStream, result) = await GetTorrentStream ( torrentPath );

            if ( !result ) {
                context.Response.StatusCode = 400;
                return;
            }

            try {
                TorrentManager manager;
                if ( m_TorrentManagers.TryGetValue ( torrentPath, out var createdManager ) ) {
                    manager = createdManager;
                } else {
                    if ( torrentStream == null ) {
                        context.Response.StatusCode = 404;
                        return;
                    }
                    torrentStream.Position = 0;
                    var torrent = await Torrent.LoadAsync ( torrentStream );
                    manager = await ClientEngine.AddStreamingAsync ( torrent, DownloadsPath );
                    await manager.StartAsync ();
                    await manager.WaitForMetadataAsync ();
                    m_TorrentManagers.TryAdd ( torrentPath, manager );
                }
                var iterator = 0;
                foreach ( var file in manager.Files ) {
                    await manager.SetFilePriorityAsync ( file, iterator == activeFileIndex ? Priority.High : Priority.DoNotDownload );
                    iterator++;
                }

                if ( m_TorrentStreams.ContainsKey ( torrentPath ) ) {
                    if ( m_TorrentStreams.TryRemove ( torrentPath, out var stream ) ) stream.Dispose ();
                }

                var currentFile = manager.Files[activeFileIndex];
                var isDownloaded = currentFile.BitField.PercentComplete >= 1.0;

                if ( !isDownloaded ) {
                    var httpStream = await manager.StreamProvider.CreateHttpStreamAsync ( currentFile, false );
                    m_TorrentStreams.TryAdd ( torrentPath, httpStream );

                    context.Response.StatusCode = 302;
                    context.Response.Headers.Location = httpStream.Uri.ToString ();
                } else {
                    context.Response.StatusCode = 302;
                    context.Response.Headers.Location = ( RuntimeInformation.IsOSPlatform ( OSPlatform.Windows ) ? "file:///" : "file://" ) + currentFile.FullPath;
                }
            } catch {
                context.Response.StatusCode = 500;
            }
        }

        public static async Task StartFullDownload ( HttpContext context ) {
            context.Response.ContentType = "text/plain";
            if ( context.Request.Query.Count != 1 ) {
                context.Response.StatusCode = 204;
                return;
            }

            var torrentPath = GetStringValueFromQuery ( "path", context );

            var (torrentStream, result) = await GetTorrentStream ( torrentPath );

            if ( !result ) {
                context.Response.StatusCode = 400;
                return;
            }

            try {
                TorrentManager manager;

                if ( m_TorrentManagers.TryGetValue ( torrentPath, out var createdManager ) ) {
                    manager = createdManager;
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
                    manager = await ClientEngine.AddStreamingAsync ( torrent, DownloadsPath );
                    await manager.StartAsync ();
                    await manager.WaitForMetadataAsync ();
                    m_TorrentManagers.TryAdd ( torrentPath, manager );
                }
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync ( "Downloading started" );
            } catch {
                context.Response.StatusCode = 500;
            }
        }

        public static async Task Finalization ( HttpContext context ) {
            if ( context is null ) throw new ArgumentNullException ( nameof ( context ) );

            await ClientEngine.StopAllAsync ();
            m_TorrentManagers.Clear ();
            m_TorrentStreams.Clear ();

            if ( Directory.Exists ( DownloadsPath ) ) Directory.Delete ( DownloadsPath, true );

            context.Response.StatusCode = 200;
            await context.Response.WriteAsync ( "Completed" );
        }

    }

}
