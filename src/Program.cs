using Microsoft.Extensions.Hosting.WindowsServices;
using System.Net;
using System.Text;
using TorrentStream;
using TorrentStream.SerializerContexts;
#if !DEBUG
using System.Runtime.InteropServices;
#endif

var listenAddress = Environment.GetEnvironmentVariable ( "LISTEN_ADDR" );
var webPortValue = Environment.GetEnvironmentVariable ( "WEB_PORT" );
var downloadDirectory = Environment.GetEnvironmentVariable ( "DOWNLOAD_PATH" );
var webPort = string.IsNullOrEmpty ( webPortValue ) ? 0 : Convert.ToInt32 ( webPortValue );
if ( webPort < 0 ) webPort = 0;

GlobalConfiguration.Port = webPort > 0 ? webPort : 5082;

GlobalConfiguration.BaseFolder = Path.GetDirectoryName ( AppContext.BaseDirectory ) ?? "";

if ( !string.IsNullOrEmpty ( downloadDirectory ) ) {
    var directoryExists = Directory.Exists ( downloadDirectory );
    if ( !directoryExists ) Console.WriteLine ( $"Directory {downloadDirectory} not found!" );

    var directoryWritable = CheckIfDirectoryIsWritable ( downloadDirectory );
    if ( !directoryWritable ) Console.WriteLine ( $"Directory {downloadDirectory} not writable or corrupt!" );

    if ( directoryExists && directoryWritable ) GlobalConfiguration.BaseFolder = downloadDirectory;
}

static bool CheckIfDirectoryIsWritable ( string downloadDirectory ) {
    try {
        File.Create ( Path.Combine ( downloadDirectory, Path.GetRandomFileName () ), 1, FileOptions.DeleteOnClose );
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

var options = new WebApplicationOptions {
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService () ? AppContext.BaseDirectory : default
};

var builder = WebApplication.CreateBuilder ( options );
builder.Services.ConfigureHttpJsonOptions ( options => {
    options.SerializerOptions.TypeInfoResolverChain.Insert ( 0, TorrentStreamSerializerContext.Default );
} );
builder.WebHost.ConfigureKestrel (
    options => {
        if ( string.IsNullOrEmpty ( listenAddress ) ) {
            options.ListenLocalhost ( GlobalConfiguration.Port );
        } else {
            options.Listen ( IPAddress.Parse ( listenAddress ), GlobalConfiguration.Port );
        }
        options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes ( 10 );
        options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes ( 5 );
    }
);
builder.Host.UseSystemd ();
builder.Host.UseWindowsService ();
var app = builder.Build ();

app.UseExceptionHandler ( "/error" );

app.UseWebSockets (
    new WebSocketOptions {
        KeepAliveInterval = TimeSpan.FromSeconds ( 30 )
    }
);

app.UseRouting ();

app.MapGet ( "/online", TorrentHandler.StartDownloadForOnlineStreaming );
app.MapGet ( "/fulldownload", TorrentHandler.StartFullDownload );
app.MapGet ( "/torrents", TorrentHandler.GetTorrents );
app.MapGet ( "/clearall", TorrentHandler.Finalization );
app.MapGet ( "/clearonlytorrent", TorrentHandler.ClearOnlyTorrent );
app.MapGet ( "/cleartorrentanddata", TorrentHandler.ClearTorrentAndData );
app.MapGet ( "/ws", TorrentHandler.TorrentWebSocket );
app.MapGet ( "/error", async ( context ) => { await context.Response.Body.WriteAsync ( Encoding.UTF8.GetBytes ( "Something went wrong :(" ) ); } );
app.MapGet ( "/proxyvideolist", ProxyHandler.ProxyVideolist );
app.MapGet ( "/proxyvideopart", ProxyHandler.ProxyVideoPart );
app.MapGet ( "/proxyvideopartfallback", ProxyHandler.ProxyVideoPartFallback );
app.MapGet ( "/playerws", ExternalPlayer.ExternalWebSocket );

await TorrentHandler.LoadState ();

app.Run ();

await TorrentHandler.SaveStateAndStop ();
