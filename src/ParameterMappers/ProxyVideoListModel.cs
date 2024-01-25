using System.Reflection;

namespace TorrentStream.ParameterMappers {
    public class ProxyVideoListModel {

        public string Path { get; set; } = "";

        public bool NeedFallback { get; set; } = false;

        public HttpContext? Context { get; set; }

        public static ValueTask<ProxyVideoListModel?> BindAsync ( HttpContext context, ParameterInfo parameter ) {
            var path = GetStringValueFromQuery ( "path", context );
            var needFallback = GetStringValueFromQuery ( "fallback", context ).ToLowerInvariant () == "true";

            var result = new ProxyVideoListModel {
                Path = path,
                Context = context,
                NeedFallback = needFallback,
            };
            return ValueTask.FromResult<ProxyVideoListModel?> ( result );
        }

        private static string GetStringValueFromQuery ( string key, HttpContext httpContext ) {
            return httpContext.Request.Query.ContainsKey ( key ) ? httpContext.Request.Query.Where ( a => a.Key == key ).FirstOrDefault ().Value.First () ?? "" : "";
        }

    }
}
