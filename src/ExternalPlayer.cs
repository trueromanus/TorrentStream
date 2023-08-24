using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace TorrentStream {

    public class ExternalPlayer {

        public static readonly ConcurrentDictionary<WebSocket, bool> m_ActiveWebSockets = new ();

        public static async Task ExternalWebSocket ( HttpContext context ) {
            if ( !context.WebSockets.IsWebSocketRequest ) context.Response.StatusCode = StatusCodes.Status400BadRequest;

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync ();
            m_ActiveWebSockets.TryAdd ( webSocket, true );
            await StartSocketSession ( webSocket );
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
                    case "role": //role

                        break;
                    case "sc": //source

                        //if ( m_TorrentManagers.Any () ) await webSocket.SendAsync ( GetDownloadStatus (), WebSocketMessageType.Text, true, CancellationToken.None );
                        break;
                    case "vm": //volume

                        //if ( m_TorrentManagers.Any () ) await webSocket.SendAsync ( GetDownloadStatus (), WebSocketMessageType.Text, true, CancellationToken.None );
                        break;
                    case "svm": //synchronization volume

                        //if ( m_TorrentManagers.Any () ) await webSocket.SendAsync ( GetDownloadStatus (), WebSocketMessageType.Text, true, CancellationToken.None );
                        break;
                    case "st": //state

                        //if ( m_TorrentManagers.Any () ) await webSocket.SendAsync ( GetDownloadStatus (), WebSocketMessageType.Text, true, CancellationToken.None );
                        break;
                    case "sst": //synchronization state

                        //if ( m_TorrentManagers.Any () ) await webSocket.SendAsync ( GetDownloadStatus (), WebSocketMessageType.Text, true, CancellationToken.None );
                        break;
                }
            }

            if ( m_ActiveWebSockets.ContainsKey ( webSocket ) ) m_ActiveWebSockets.TryRemove ( webSocket, out var _ );
        }

    }

}
