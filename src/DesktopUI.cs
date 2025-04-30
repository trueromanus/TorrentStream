using EmptyFlow.SciterAPI;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace TorrentStream {

    /// <summary>
    /// Desktop UI.
    /// </summary>
    public static class DesktopUI {

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
                using var stream = Assembly.GetExecutingAssembly ().GetManifestResourceStream ( libraryName );
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
                    host.Process ();
                }
            );
        }

    }

}
