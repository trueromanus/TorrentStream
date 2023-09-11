using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace TorrentStream {

    public class ExternalPlayer {

        public static readonly ConcurrentDictionary<WebSocket, string> m_ActiveWebSockets = new ();

        public static readonly ConcurrentDictionary<WebSocket, bool> m_NewWebSockets = new ();

        private const string SourceCommand = "sc";

        private const string VolumeCommand = "vm";

        private const string StateCommand = "st";

        private const string MuteCommand = "mt";

        private const string SeekCommand = "sk";

        private const string RoleCommand = "ro";

        private const string SynchronizationStateCommand = "sst";

        private const string SynchronizationVolumeCommand = "svm";

        private const string SynchronizationSeekCommand = "ssk";

        private const string SynchronizationMuteCommand = "smt";

        private static readonly HashSet<string> m_commands = new () {
            SourceCommand,
            VolumeCommand,
            StateCommand,
            SynchronizationStateCommand,
            SynchronizationVolumeCommand,
            MuteCommand,
            SeekCommand,
            SynchronizationSeekCommand,
            SynchronizationMuteCommand
        };

        public static async Task ExternalWebSocket ( HttpContext context ) {
            if ( !context.WebSockets.IsWebSocketRequest ) {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync ();
            m_NewWebSockets.TryAdd ( webSocket, true );
            await StartSocketSession ( webSocket );
        }

        private static ReadOnlyMemory<byte> GetMessageAsBytes ( string command, string message ) => Encoding.UTF8.GetBytes ( $"{command}:{message}" ).AsMemory ();

        private static IEnumerable<WebSocket> GetOtherSockets ( WebSocket sender, string recipient = "" ) {
            return m_ActiveWebSockets
                .Where ( a => a.Key != sender && a.Key.State == WebSocketState.Open && ( string.IsNullOrEmpty ( recipient ) || a.Value == recipient ) )
                .Select ( a => a.Key )
                .ToList ();
        }

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
                if ( parts.Length == 3 ) {
                    var recepient = parts[0];
                    var command = parts[1];
                    var parameter = parts[2];

                    if ( !m_commands.Contains ( command ) ) continue;

                    foreach ( var socket in GetOtherSockets ( webSocket, recepient ) ) await SendToSocket ( socket, command, parameter );
                }

                if ( parts.Length == 2 ) {
                    var command = parts[0];
                    var parameter = parts[1];

                    if ( !m_NewWebSockets.ContainsKey ( webSocket ) ) continue;
                    if ( command != RoleCommand ) continue;

                    if ( !m_ActiveWebSockets.TryAdd ( webSocket, parameter ) ) m_ActiveWebSockets.TryAdd ( webSocket, parameter );
                    if ( !m_NewWebSockets.TryRemove ( webSocket, out _ ) ) m_NewWebSockets.TryRemove ( webSocket, out _ );
                }
            }

            if ( m_ActiveWebSockets.ContainsKey ( webSocket ) ) m_ActiveWebSockets.TryRemove ( webSocket, out var _ );
        }

    }

}
