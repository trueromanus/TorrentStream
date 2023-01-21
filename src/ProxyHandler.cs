using System.IO;
using System.Text;

namespace TorrentStream {

    public static class ProxyHandler {

        private static string GetStringValueFromQuery ( string key, HttpContext httpContext ) {
            return httpContext.Request.Query.ContainsKey ( key ) ? httpContext.Request.Query.Where ( a => a.Key == key ).FirstOrDefault ().Value.First () ?? "" : "";
        }

        public static async Task ProxyVideolist ( HttpContext context ) {
            if ( context.Request.Query.Count != 1 ) {
                context.Response.StatusCode = 404;
                return;
            }

            var path = GetStringValueFromQuery ( "path", context );

            var httpClient = new HttpClient ();
            httpClient.DefaultRequestHeaders.Add ( "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36" );
            var medialist = await httpClient.GetStringAsync ( path );

            var uri = new Uri ( path );
            var partPrefix = $"{uri.Scheme}://{uri.Host}";
            var result = new List<string> ();
            foreach ( var line in medialist.Split ( "\n" ) ) {
                if ( line.StartsWith ( partPrefix ) ) {
                    result.Add ( $"http://localhost:{GlobalConfiguration.Port}/proxyvideopart?path=" + line );
                } else {
                    result.Add ( line );
                }
            }

            await context.Response.Body.WriteAsync ( Encoding.UTF8.GetBytes ( string.Join ( '\n', result ) ) );
        }

        public static async Task ProxyVideoPart ( HttpContext context ) {
            context.Response.StatusCode = 404;
            if ( context.Request.Query.Count != 1 ) return;

            var path = GetStringValueFromQuery ( "path", context );

            var httpClient = new HttpClient ();
            httpClient.DefaultRequestHeaders.Add ( "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36" );
            var videopart = await httpClient.GetStreamAsync ( path );

            context.Response.ContentType = "video/mp2t";
            context.Response.Headers.Add( "Content-Disposition", "attachment; filename=file.ts" );
            context.Response.StatusCode = 200;

            var buffer = new byte[1024 * 2];
            while ( true ) {
                var bytesCount = await videopart.ReadAsync ( buffer, 0, buffer.Length );
                if ( bytesCount == 0 ) break;
                await context.Response.Body.WriteAsync ( buffer, 0, bytesCount );
            }

            await context.Response.Body.FlushAsync();
            context.Response.Body.Close ();
        }

    }

}
