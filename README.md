# TorrentStream
Simple web server for streaming torrent files in video players (like VLC, mpv, MPC-HC and so on)

# Documentation
[English Documentation](https://github.com/trueromanus/TorrentStream/wiki/En-Documentation)  
[Russian Documentation](https://github.com/trueromanus/TorrentStream/wiki/Ru-Documentation)

# Install on linux
[NixOS](https://github.com/trueromanus/TorrentStream/wiki/Nix-install)

# Build Requirements
- DotNet 8.0+
# Build Instructions
```shell
dotnet dotnet publish -r <platform-indetifier> -c Release --self-contained true src/TorrentStream.csproj
```
`platform-identifier` can be:
- osx-x64 (macOS with intel processor)
- osx-arm64 (macOS with M1+ processor)
- win-arm64
- win-x64
- linux-arm64
- linux-x64
