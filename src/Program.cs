#if !DEBUG
using System.Runtime.InteropServices;
#endif
using System.Text;
using TorrentStream;

var webPortValue = Environment.GetEnvironmentVariable ( "WEB_PORT" );
var webPort = string.IsNullOrEmpty ( webPortValue ) ? 0 : Convert.ToInt32 ( webPortValue );
if ( webPort < 0 ) webPort = 0;

#if !DEBUG
if ( RuntimeInformation.IsOSPlatform ( OSPlatform.Windows ) ) {
    WindowsExtras.AdjustConsoleWindow ( args.Any ( a => a.ToLowerInvariant () == "showconsole" ) );
}
#endif

var builder = WebApplication.CreateBuilder ( args );
builder.WebHost.ConfigureKestrel (
    options => {
        options.ListenLocalhost ( webPort > 0 ? webPort : 5082 );
        options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes ( 10 );
        options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes ( 5 );
    }
);
var app = builder.Build ();

app.UseExceptionHandler ( "/error" );

app.UseWebSockets (
    new WebSocketOptions {
        KeepAliveInterval = TimeSpan.FromSeconds( 30 )
    }
);

app.UseRouting ();

app.MapGet ( "/online", TorrentHandler.StartDownloadForOnlineStreaming );
app.MapGet ( "/fulldownload", TorrentHandler.StartFullDownload );
app.MapGet ( "/clearall", TorrentHandler.Finalization );
app.MapGet ( "/ws", TorrentHandler.TorrentWebSocket );
app.MapGet ( "/error", async ( context ) => { await context.Response.Body.WriteAsync ( Encoding.UTF8.GetBytes ( "Something went wrong :(" ) ); } );

await TorrentHandler.LoadState ();

app.Run ();

await TorrentHandler.SaveState ();
