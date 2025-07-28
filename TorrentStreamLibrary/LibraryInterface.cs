using System.Runtime.InteropServices;
using TorrentStream;

namespace TorrentStreamLibrary {

    public static class LibraryInterface {

        [UnmanagedCallersOnly ( EntryPoint = "initializetorrentstream" )]
        public static int initializetorrentstream ( int port, IntPtr downloadPath, IntPtr listenAddress, bool showui ) => InitializeTorrentStreamInternal ( port, downloadPath, listenAddress, showui );

        public static int InitializeTorrentStreamInternal ( int port, IntPtr downloadPathPointer, IntPtr listenAddressPointer, bool showui ) {
            if ( port <= 0 || port > 65534 ) return 1;

            var downloadPath = Marshal.PtrToStringUTF8 ( downloadPathPointer );
            if ( string.IsNullOrEmpty ( downloadPath ) ) return 2;

            var listenAddress = Marshal.PtrToStringUTF8 ( downloadPathPointer );

            GlobalConfiguration.Port = port;
            GlobalConfiguration.BaseFolder = downloadPath;
            if ( !string.IsNullOrEmpty ( listenAddress ) ) GlobalConfiguration.ListenAddress = listenAddress;
            GlobalConfiguration.ShowUI = showui;

            Task.Run (
                async () => {
                    await WebServer.Initialize ( [] );
                    WebServer.Run ();
                }
            );

            return 0;
        }

        [UnmanagedCallersOnly ( EntryPoint = "stoptorrentstream" )]
        public static void stoptorrentstream () => StopTorrentStreamInternal ();

        public static void StopTorrentStreamInternal () => Task.Run ( WebServer.ForceStop );

    }

}
