using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace TorrentStream {

    public class ExternalPlayer {

        public static readonly ConcurrentDictionary<WebSocket, bool> m_ActiveWebSockets = new ();

        private const string SourceCommand = "sc";

        private const string VolumeCommand = "vm";

        private const string StateCommand = "st";

        private const string SynchronizationStateCommand = "sst";

        private const string SynchronizationVolumeCommand = "svm";

        private static readonly HashSet<string> m_commands = new () {
            SourceCommand,
            VolumeCommand,
            StateCommand,
            SynchronizationStateCommand,
            SynchronizationVolumeCommand
        };

        public static async Task ExternalWebSocket ( HttpContext context ) {
            if ( !context.WebSockets.IsWebSocketRequest ) {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync ();
            m_ActiveWebSockets.TryAdd ( webSocket, true );
            await StartSocketSession ( webSocket );
        }

        private static ReadOnlyMemory<byte> GetMessageAsBytes ( string command, string message ) => Encoding.UTF8.GetBytes ( $"{command}:{message}" ).AsMemory ();

        private static IEnumerable<WebSocket> GetOtherSockets ( WebSocket sender ) => m_ActiveWebSockets.Keys.Where ( a => sender != a && a.State == WebSocketState.Open );

        private static async Task SendToSocket ( WebSocket socket, string command, string message ) {
            await socket.SendAsync ( GetMessageAsBytes ( command, message ), WebSocketMessageType.Text, true, CancellationToken.None );
        }

        private static async Task StartSocketSession ( WebSocket webSocket ) {
            var buffer = new byte[1024].AsMemory ();

            while ( true ) {
                if ( webSocket.State != WebSocketState.Open ) break;

                var receiveResult = await webSocket.ReceiveAsync ( buffer, CancellationToken.None );

                if ( receiveResult.Count == 0 ) break;

                var messageContent = Encoding.UTF8.GetString ( buffer[..receiveResult.Count].ToArray () );
                var parts = messageContent.Split ( ":" );
                if ( parts.Length != 2 ) continue;

                var command = parts[0];
                var parameter = parts[1];

                if ( !m_commands.Contains ( command ) ) continue;

                foreach ( var socket in GetOtherSockets ( webSocket ) ) await SendToSocket ( socket, command, parameter );
            }

            if ( m_ActiveWebSockets.ContainsKey ( webSocket ) ) m_ActiveWebSockets.TryRemove ( webSocket, out var _ );
        }

    }

}
