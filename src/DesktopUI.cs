using EmptyFlow.SciterAPI;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace TorrentStream {

    /// <summary>
    /// Desktop UI.
    /// </summary>
    public static class DesktopUI {

        private const string CommandPrefix = "command.dat/?";

        private const string CommandStart = "start";

        private const string CommandEnd = "stop";

        private const string CommandDelete = "delete";

        private static string m_itemsCache = "[]";

        public static void SetCache ( string value ) => m_itemsCache = string.IsNullOrEmpty ( value ) ? "[]" : value;

        private static string SaveSciter () {
            var appPath = Path.Combine ( Environment.GetFolderPath ( Environment.SpecialFolder.LocalApplicationData ), "TorrentStream" );
            if ( !Directory.Exists ( appPath ) ) Directory.CreateDirectory ( appPath );

            if ( RuntimeInformation.IsOSPlatform ( OSPlatform.Windows ) ) {
                SaveLibraryToAppFolder ( appPath, "sciter.dll" );
            }
            if ( RuntimeInformation.IsOSPlatform ( OSPlatform.Linux ) ) {
                SaveLibraryToAppFolder ( appPath, "libsciter.so" );
            }
            if ( RuntimeInformation.IsOSPlatform ( OSPlatform.OSX ) ) {
                SaveLibraryToAppFolder ( appPath, "libsciter.dylib" );
            }

            static void SaveLibraryToAppFolder ( string appPath, string libraryName ) {
                using var stream = typeof ( DesktopUI ).Assembly.GetManifestResourceStream ( libraryName );
                if ( stream == null ) throw new NotSupportedException ( "Not found sciter library in embedded resources!" );

                var filePath = Path.Combine ( appPath, libraryName );
                if ( File.Exists ( filePath ) ) File.Delete ( filePath );

                using var file = File.OpenWrite ( filePath );
                stream.CopyTo ( file );
            }

            return appPath;
        }

        private static byte[] ProtocolHandler ( string path ) {
            var fileName = path.Replace ( "http://", "" );
            if ( fileName.Contains ( "index.htm/" ) ) fileName = fileName.Replace ( "index.htm/", "" );

            if ( fileName == "data.json" ) return Encoding.UTF8.GetBytes ( m_itemsCache ); // data json is dynamic route
            if ( fileName.StartsWith ( CommandPrefix ) ) {
                var parameters = fileName.Replace ( CommandPrefix, "" ).Split ( "&" );
                var command = parameters.First ();
                var parameter1 = parameters.ElementAt ( 1 );

                switch ( command ) {
                    case CommandStart:
                        TorrentHandler.StartTorrent ( parameter1 );
                        break;
                    case CommandEnd:
                        TorrentHandler.StopTorrent ( parameter1 );
                        break;
                    case CommandDelete:
                        TorrentHandler.DeleteTorrent ( parameter1 );
                        break;
                    default:
                        break;
                }
            }

            var assembly = Assembly.GetExecutingAssembly ();
            if ( assembly.GetManifestResourceNames ().Contains ( fileName ) ) {
                using var stream = assembly.GetManifestResourceStream ( fileName );
                if ( stream == null ) return [];

                var output = new MemoryStream ();
                stream.CopyTo ( output );
                output.Position = 0;
                return output.ToArray ();
            }
            return [];
        }

        public static void StartTimerForRefreshTorrentsData () {
            Task.Run (
                async () => {
                    try {
                        m_itemsCache = await TorrentHandler.GetTorrentsAsJson ();
                    } catch ( Exception ex ) {
                        Console.WriteLine ( ex );
                    }

                    while ( true ) {
                        try {
                            m_itemsCache = await TorrentHandler.GetTorrentsAsJson ();
                        } catch ( Exception ex ) {
                            Console.WriteLine ( ex );
                        }
                        await Task.Delay ( 1500 );
                    }
                }
            );
        }

        public static void Run () {
            var path = SaveSciter ();
            SciterLoader.Initialize ( path );

            Task.Run (
                () => {
                    var host = new SciterAPIHost ();
                    host.LoadAPI ();
                    host.CreateMainWindow ( 300, 300, enableDebug: true, enableFeature: true );
                    host.Callbacks.AddProtocolHandler ( "http://", ProtocolHandler );
                    host.LoadFile ( "http://index.htm" );
                    StartTimerForRefreshTorrentsData ();
                    host.Process ();
                }
            );
        }

        public static string ComputeSha256Hash ( string value ) {
            using var sha256Hash = SHA256.Create ();
            var bytes = sha256Hash.ComputeHash ( Encoding.UTF8.GetBytes ( value ) );

            return Convert.ToBase64String ( bytes );
        }

    }

}
