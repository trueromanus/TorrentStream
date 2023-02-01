using Microsoft.Extensions.Hosting.WindowsServices;
using System.Text;
using TorrentStream;

var webPortValue = Environment.GetEnvironmentVariable ( "WEB_PORT" );
var webPort = string.IsNullOrEmpty ( webPortValue ) ? 0 : Convert.ToInt32 ( webPortValue );
if ( webPort < 0 ) webPort = 0;

GlobalConfiguration.Port = webPort > 0 ? webPort : 5082;

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
builder.WebHost.ConfigureKestrel (
    options => {
        options.ListenLocalhost ( GlobalConfiguration.Port );
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
app.MapGet ( "/ws", TorrentHandler.TorrentWebSocket );
app.MapGet ( "/error", async ( context ) => { await context.Response.Body.WriteAsync ( Encoding.UTF8.GetBytes ( "Something went wrong :(" ) ); } );
app.MapGet ( "/proxyvideolist", ProxyHandler.ProxyVideolist );
app.MapGet ( "/proxyvideopart", ProxyHandler.ProxyVideoPart );

await TorrentHandler.LoadState ();

app.Run ();

await TorrentHandler.SaveState ();
