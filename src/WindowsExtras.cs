using System.Runtime.InteropServices;

namespace TorrentStream {

    public static class WindowsExtras {

        [DllImport ( "kernel32.dll" )]
        static extern IntPtr GetConsoleWindow ();

        [DllImport ( "user32.dll" )]
        static extern bool ShowWindow ( IntPtr hWnd, int nCmdShow );

        private const int NotVisible = 0;

        private const int Visible = 5;

        public static void AdjustConsoleWindow ( bool visible ) {
            var handle = GetConsoleWindow ();

            ShowWindow ( handle, visible ? Visible : NotVisible );
        }

    }
}
