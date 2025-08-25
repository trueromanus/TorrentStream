using TorrentStream;
#if !DEBUG
using System.Runtime.InteropServices;
#endif

var listenAddress = Environment.GetEnvironmentVariable ( "LISTEN_ADDR" );
var webPortValue = Environment.GetEnvironmentVariable ( "WEB_PORT" );
var downloadDirectory = Environment.GetEnvironmentVariable ( "DOWNLOAD_PATH" );
var interactive = Environment.GetEnvironmentVariable ( "INTERACTIVE" );
var webPort = string.IsNullOrEmpty ( webPortValue ) ? 0 : Convert.ToInt32 ( webPortValue );
if ( webPort < 0 ) webPort = 0;

GlobalConfiguration.Port = webPort > 0 ? webPort : 5082;

GlobalConfiguration.BaseFolder = Path.GetDirectoryName ( AppContext.BaseDirectory ) ?? "";

GlobalConfiguration.ListenAddress = listenAddress;

GlobalConfiguration.ShowUI = interactive?.ToLowerInvariant () == "enabled";
#if DEBUG
GlobalConfiguration.ShowUI = true;
#endif

if ( !string.IsNullOrEmpty ( downloadDirectory ) ) {
    var directoryExists = Directory.Exists ( downloadDirectory );
    if ( !directoryExists ) Console.WriteLine ( $"Directory {downloadDirectory} not found!" );

    var directoryWritable = await CheckIfDirectoryIsWritable ( downloadDirectory );
    if ( !directoryWritable ) Console.WriteLine ( $"Directory {downloadDirectory} not writable or corrupt!" );

    if ( directoryExists && directoryWritable ) GlobalConfiguration.BaseFolder = downloadDirectory;
}

static async Task<bool> CheckIfDirectoryIsWritable ( string downloadDirectory ) {
    try {
        var randomPath = Path.Combine ( downloadDirectory, Path.GetRandomFileName () );
        await File.WriteAllTextAsync ( randomPath, " " );
        File.Delete ( randomPath );
        return true;
    } catch {
        return false;
    }
}

#if !DEBUG
if ( RuntimeInformation.IsOSPlatform ( OSPlatform.Windows ) ) {
    var noConsole = args.Any ( a => a.ToLowerInvariant () == "-noconsole" );
    WindowsExtras.AdjustConsoleWindow ( !noConsole );
}
#endif

await WebServer.Initialize ( args );
WebServer.Run ();
await WebServer.Stop ();