using TorrentStream;

var builder = WebApplication.CreateBuilder ( args );
var app = builder.Build ();

app.UseRouting ();

app.MapGet ( "/online", TorrentHandler.StartDownloadForOnlineStreaming );

AppDomain.CurrentDomain.ProcessExit += new EventHandler ( CurrentDomainProcessExit );

void CurrentDomainProcessExit ( object? sender, EventArgs e ) {
    Console.WriteLine ( "Finilization torrents" );
    TorrentHandler.Finalization ();
}

app.Run ();
