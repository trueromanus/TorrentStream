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

app.UseRouting ();

app.MapGet ( "/online", TorrentHandler.StartDownloadForOnlineStreaming );
app.MapGet ( "/clearall", TorrentHandler.Finalization );

app.Run ();
