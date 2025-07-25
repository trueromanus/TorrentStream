﻿using System.Net;
using System.Text;
using TorrentStream.ParameterMappers;

namespace TorrentStream {

    public static class ProxyHandler {

        private static string GetStringValueFromQuery ( string key, HttpContext httpContext ) {
            return httpContext.Request.Query.ContainsKey ( key ) ? httpContext.Request.Query.Where ( a => a.Key == key ).FirstOrDefault ().Value.First () ?? "" : "";
        }

        public static async Task ProxyVideolist ( ProxyVideoListModel model ) {
            if ( model.Context == null ) return;

            if ( string.IsNullOrEmpty ( model.Path ) ) return;

            var pathIndex = model.Context.Request.QueryString.Value?.IndexOf ( "path=" ) ?? -1;
            if ( pathIndex == -1 ) return;

            var queryString = model.Context.Request.QueryString.Value ?? "";
            var originalPath = queryString.Substring ( pathIndex + 5 );
            if ( originalPath.Any () && originalPath != model.Path ) model.Path = originalPath;

            var httpClient = new HttpClient ();
            httpClient.DefaultRequestHeaders.Add ( "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36" );
            var response = await httpClient.GetAsync ( model.Path );
            var medialist = await response.Content.ReadAsStringAsync ();

            var uri = new Uri ( model.Path );
            var partPrefix = $"{uri.Scheme}://{uri.Host}";
            var result = new List<string> ();
            foreach ( var line in medialist.Split ( "\n" ) ) {
                if ( line.StartsWith ( partPrefix ) ) {
                    result.Add ( $"http://localhost:{GlobalConfiguration.Port}/{( model.NeedFallback ? "proxyvideopartfallback" : "proxyvideopart" )}?path=" + line );
                } else {
                    result.Add ( line );
                }
            }

            model.Context.Response.ContentType = response.Content?.Headers?.ContentType?.MediaType ?? "application/x-mpegURL";
            await model.Context.Response.Body.WriteAsync ( Encoding.UTF8.GetBytes ( string.Join ( '\n', result ) ) );
        }

        public static async Task ProxyVideoPart ( HttpContext context ) {
            context.Response.StatusCode = 404;
            if ( context.Request.Query.Count != 1 ) return;

            var path = GetStringValueFromQuery ( "path", context );

            Stream videopart;
            try {
                videopart = await GetVideoPart ( path );
            } catch {
                videopart = await GetVideoPart ( path );
            }

            context.Response.ContentType = "video/mp2t";
            context.Response.Headers["Content-Disposition"] = "attachment; filename=file.ts";
            context.Response.StatusCode = 200;

            var buffer = new byte[1024 * 2];
            while ( true ) {
                var bytesCount = await videopart.ReadAsync ( buffer, 0, buffer.Length );
                if ( bytesCount == 0 ) break;
                await context.Response.Body.WriteAsync ( buffer, 0, bytesCount );
            }
        }

        private static async Task<Stream> GetVideoPart ( string path ) {
            var httpClient = new HttpClient ();
            httpClient.DefaultRequestHeaders.Add ( "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36 AniLibriaQt/1.0.0" );
            var videopart = await httpClient.GetStreamAsync ( path );
            return videopart;
        }

        private static async Task CopyStreamToResponse ( HttpContext context, MemoryStream stream ) {
            stream.Position = 0;
            await stream.CopyToAsync ( context.Response.Body );
            await context.Response.Body.FlushAsync ();
            context.Response.Body.Close ();
        }

        public static async Task ProxyVideoPartFallback ( HttpContext context ) {
            if ( context.Request.Query.Count != 1 ) {
                context.Response.StatusCode = 404;
                return;
            }

            var path = GetStringValueFromQuery ( "path", context );

            context.Response.ContentType = "video/mp2t";
            context.Response.Headers["Content-Disposition"] = "attachment; filename=file.ts";
            context.Response.StatusCode = 200;

            try {
                var stream = await TryGetVideoPart ( path, useHttp2: true );
                await CopyStreamToResponse ( context, stream );
            } catch {
                Console.WriteLine ( "Try fallback to HTTP1.1" );
                var stream = await TryGetVideoPart ( path, useHttp2: false ); // falback to HTTP1.1
                await CopyStreamToResponse ( context, stream );
            }
        }

        private static async Task<MemoryStream> TryGetVideoPart ( string path, bool useHttp2 ) {
            var httpClient = new HttpClient ();
            if ( useHttp2 ) {
                httpClient.DefaultRequestVersion = HttpVersion.Version20;
                httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            }
            httpClient.DefaultRequestHeaders.Add ( "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36 AniLibriaQt/1.0.0" );
            var videopart = await httpClient.GetStreamAsync ( path );
            var result = new MemoryStream ();
            await videopart.CopyToAsync ( result );
            return result;
        }
    }

}
