using Microsoft.Extensions.Hosting.WindowsServices;
using System.Net;
using System.Text;
using TorrentStream.SerializerContexts;

namespace TorrentStream {

    public static class WebServer {

        private static WebApplication? m_application;

        public static async Task Initialize ( string[] args ) {
            var version = DesktopUI.GetCurrentAssembly ().GetName ().Version;
            if ( version != null ) Console.WriteLine ( $"TorrentStream version {version.Major}.{version.Minor}.{version.Build}.{version.Revision}" );

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
                    if ( string.IsNullOrEmpty ( GlobalConfiguration.ListenAddress ) ) {
                        options.ListenLocalhost ( GlobalConfiguration.Port );
                    } else {
                        options.Listen ( IPAddress.Parse ( GlobalConfiguration.ListenAddress ), GlobalConfiguration.Port );
                    }
                    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes ( 10 );
                    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes ( 5 );
                }
            );
            builder.Host.UseSystemd ();
            builder.Host.UseWindowsService ();
            var app = builder.Build ();

            m_application = app;

            app.UseExceptionHandler ( "/error" );

            app.UseWebSockets (
                new WebSocketOptions {
                    KeepAliveInterval = TimeSpan.FromSeconds ( 30 )
                }
            );

            app.UseRouting ();

            TorrentHandler.Initialize ();

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

            if ( GlobalConfiguration.ShowUI ) DesktopUI.Run ();
        }

        public static void Run () {
            m_application?.Run ();
        }

        public static async Task ForceStop () {
            if ( m_application != null ) await m_application.StopAsync ();

            await TorrentHandler.SaveStateAndStop ();
        }

        public static Task Stop () => TorrentHandler.SaveStateAndStop ();

    }

}
