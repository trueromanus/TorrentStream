& "dotnet" publish -c Release -r win-x64 --self-contained true
Compress-Archive -Path bin\Release\net8.0\win-x64\publish\TorrentStream.exe -DestinationPath win-x64.zip
& "dotnet" publish -c Release -r win-arm64 --self-contained true
Compress-Archive -Path bin\Release\net8.0\win-arm64\publish\TorrentStream.exe -DestinationPath win-arm64.zip
& "dotnet" publish -c Release -r osx-x64 --self-contained true
Compress-Archive -Path bin\Release\net8.0\osx-x64\publish\TorrentStream -DestinationPath osx-x64.zip
& "dotnet" publish -c Release -r osx-arm64 --self-contained true
Compress-Archive -Path bin\Release\net8.0\osx-arm64\publish\TorrentStream -DestinationPath osx-arm64.zip
& "dotnet" publish -c Release -r linux-x64 --self-contained true
Compress-Archive -Path bin\Release\net8.0\linux-x64\publish\TorrentStream -DestinationPath linux-x64.zip
& "dotnet" publish -c Release -r linux-arm64 --self-contained true
Compress-Archive -Path bin\Release\net8.0\linux-arm64\publish\TorrentStream -DestinationPath linux-arm64.zip
