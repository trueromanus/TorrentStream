using System.Runtime.InteropServices;
using System.Text;
using TorrentStream;

namespace TorrentStreamLibrary {

    public static class LibraryInterface {

        public delegate void CallbackUpdateTorrents ( bool isResult );

        public delegate void CallbackConnected ();

        public delegate void CallbackFullDownloadStarted ( int id, nint downloadPath, bool isAdded );

        private static List<byte> m_zeroBytes = [0, 0, 0, 0];

        private static string GetStringFromPointer ( nint pointer ) {
            if ( RuntimeInformation.IsOSPlatform ( OSPlatform.Linux ) || RuntimeInformation.IsOSPlatform ( OSPlatform.OSX ) ) {
                if ( pointer == nint.Zero ) return "";

                var buffer = new List<byte> ();
                var offset = 0;
                while ( true ) {
                    var readedByte = Marshal.ReadByte ( pointer, offset );
                    offset++;

                    buffer.Add ( readedByte );

                    if ( buffer.Count () % 4 == 0 && buffer[^4..].SequenceEqual ( m_zeroBytes ) ) break;
                }

                if ( buffer.Count () == 4 && buffer.SequenceEqual ( m_zeroBytes ) ) return "";

                return Encoding.UTF8.GetString ( buffer[..^4].ToArray () );
            }

            return Marshal.PtrToStringUni ( pointer ) ?? "";
        }

        [UnmanagedCallersOnly ( EntryPoint = "initializetorrentstream" )]
        public static int initializetorrentstream ( int port, IntPtr downloadPath, IntPtr listenAddress, bool showui, nint callbackPointer ) => InitializeTorrentStreamInternal ( port, downloadPath, listenAddress, showui, callbackPointer );
        public static int InitializeTorrentStreamInternal ( int port, IntPtr downloadPathPointer, IntPtr listenAddressPointer, bool showui, nint callbackPointer ) {
            if ( port <= 0 || port > 65534 ) return 1;

            var downloadPath = GetStringFromPointer ( downloadPathPointer );
            if ( string.IsNullOrEmpty ( downloadPath ) ) return 2;

            var listenAddress = GetStringFromPointer ( listenAddressPointer );

            GlobalConfiguration.Port = port;
            GlobalConfiguration.BaseFolder = downloadPath;
            if ( !string.IsNullOrEmpty ( listenAddress ) ) GlobalConfiguration.ListenAddress = listenAddress;
            GlobalConfiguration.ShowUI = showui;

            Console.WriteLine ( $"TorrentStream library configuration: port {port}, path {downloadPath}" );

            var callback = Marshal.GetDelegateForFunctionPointer<CallbackConnected> ( callbackPointer );

            _ = Task.Run (
                async () => {
                    try {
                        await WebServer.Initialize ( [] );
                        callback ();
                        WebServer.Run ();
                    } catch ( Exception e ) {
                        Console.WriteLine ( e );
                    }
                }
            );

            return 0;
        }

        [UnmanagedCallersOnly ( EntryPoint = "stoptorrentstream" )]
        public static void stoptorrentstream () => StopTorrentStreamInternal ();
        public static void StopTorrentStreamInternal () => Task.Run ( WebServer.ForceStop );

        [UnmanagedCallersOnly ( EntryPoint = "torrentstreamclearall" )]
        public static void torrentstreamclearall ( nint callback ) => TorrentStreamClearAllInternal ( callback );
        public static void TorrentStreamClearAllInternal ( nint callback ) {
            var callbackDelegate = Marshal.GetDelegateForFunctionPointer<CallbackUpdateTorrents> ( callback );
            Task.Run (
                async () => {
                    await TorrentHandler.ClearAllTorrents ();
                    callbackDelegate ( true );
                }
            );
        }

        [UnmanagedCallersOnly ( EntryPoint = "torrentstreamclearonlytorrent" )]
        public static void torrentstreamclearonlytorrent ( nint downloadPathPointer, nint callback ) => TorrentStreamClearOnlyTorrentInternal ( downloadPathPointer, callback );
        public static void TorrentStreamClearOnlyTorrentInternal ( nint downloadPathPointer, nint callback ) {
            var callbackDelegate = Marshal.GetDelegateForFunctionPointer<CallbackUpdateTorrents> ( callback );
            var downloadPath = GetStringFromPointer ( downloadPathPointer );
            if ( string.IsNullOrEmpty ( downloadPath ) ) return;

            Task.Run (
                async () => {
                    var result = await TorrentHandler.ClearOnlyTorrentByPath ( downloadPath );
                    callbackDelegate ( result );
                }
            );
        }

        [UnmanagedCallersOnly ( EntryPoint = "torrentstreamclearonlytorrentanddata" )]
        public static void torrentstreamclearonlytorrentanddata ( nint downloadPathPointer, nint callback ) => TorrentStreamClearOnlyTorrentAndDataInternal ( downloadPathPointer, callback );
        public static void TorrentStreamClearOnlyTorrentAndDataInternal ( nint downloadPathPointer, nint callback ) {
            var callbackDelegate = Marshal.GetDelegateForFunctionPointer<CallbackUpdateTorrents> ( callback );
            var downloadPath = Marshal.PtrToStringUni ( downloadPathPointer );
            if ( string.IsNullOrEmpty ( downloadPath ) ) return;

            Task.Run (
                async () => {
                    var result = await TorrentHandler.ClearTorrentAndDataByPath ( downloadPath );
                    callbackDelegate ( result );
                }
            );
        }

        [UnmanagedCallersOnly ( EntryPoint = "torrentstreamsavestate" )]
        public static void torrentstreamsavestate ( nint downloadPathPointer, nint callback ) => TorrentStreamSaveStateInternal ();
        public static void TorrentStreamSaveStateInternal () => Task.Run ( TorrentHandler.SaveState );

        [UnmanagedCallersOnly ( EntryPoint = "torrentstreamgetall" )]
        public static nint torrentstreamgetall () => TorrentStreamGetAllInternal ();
        public static nint TorrentStreamGetAllInternal () => Marshal.StringToHGlobalUni ( TorrentHandler.GetTorrentsJson () );

        [UnmanagedCallersOnly ( EntryPoint = "torrentstreamstartdownload" )]
        public static void torrentstreamstartdownload ( int id, nint path, nint callback ) => TorrentStreamStartDownloadInternal ( id, path, callback );
        public static void TorrentStreamStartDownloadInternal ( int id, nint path, nint callback ) {
            var callbackDelegate = Marshal.GetDelegateForFunctionPointer<CallbackFullDownloadStarted> ( callback );
            var downloadPath = Marshal.PtrToStringUni ( path );
            if ( string.IsNullOrEmpty ( downloadPath ) ) return;

            Task.Run (
                async () => {
                    var result = await TorrentHandler.StartFullDownload ( downloadPath, id );

                    var pathPointer = Marshal.StringToHGlobalUni ( downloadPath );
                    callbackDelegate ( id, pathPointer, result == TorrentHandler.FullDownloadResult.NoError );
                }
            );
        }

    }

}
