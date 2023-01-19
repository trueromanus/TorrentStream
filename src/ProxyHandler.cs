namespace TorrentStream {

    public static class ProxyHandler {

        private static string GetStringValueFromQuery ( string key, HttpContext httpContext ) {
            return httpContext.Request.Query.ContainsKey ( key ) ? httpContext.Request.Query.Where ( a => a.Key == key ).FirstOrDefault ().Value.First () ?? "" : "";
        }

        public static async Task TorrentWebSocket ( HttpContext context ) {
            if ( context.Request.Query.Count != 1 ) {
                context.Response.StatusCode = 204;
                return;
            }

            var path = GetStringValueFromQuery ( "path", context );

            var httpClient = new HttpClient ();
            var stream = await httpClient.GetStreamAsync ( "https://cache.libria.fun" + path );

            await stream.CopyToAsync ( context.Response.Body );
        }

    }

}
