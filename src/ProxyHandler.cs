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
            var medialist = await httpClient.GetStringAsync ( path );

            var uri = new Uri ( path );
            var partPrefix = uri.Scheme + uri.Host;
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
            var videopart = await httpClient.GetStreamAsync ( path );

            await videopart.CopyToAsync ( context.Response.Body );
        }

    }

}
