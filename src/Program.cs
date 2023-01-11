using System.Text;
using TorrentStream;

var builder = WebApplication.CreateBuilder ( args );
builder.WebHost.ConfigureKestrel (
    options => {
        options.ListenLocalhost ( 5082 );
        options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes ( 10 );
        options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes ( 5 );
    }
);
var app = builder.Build ();

app.UseExceptionHandler ("/error");

app.UseRouting ();

app.MapGet ( "/online", TorrentHandler.StartDownloadForOnlineStreaming );
app.MapGet ( "/clearall", TorrentHandler.Finalization );
app.MapGet ( "/error", async ( context ) => { await context.Response.Body.WriteAsync ( Encoding.UTF8.GetBytes ( "Something went wrong :(" ) ); } );

app.Run ();
