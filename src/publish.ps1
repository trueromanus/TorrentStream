& "dotnet" publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
Compress-Archive -Path bin\Release\net7.0\win-x64\publish\TorrentStream.exe -DestinationPath win-x64.zip
& "dotnet" publish -c Release -r win-arm -p:PublishSingleFile=true --self-contained true
Compress-Archive -Path bin\Release\net7.0\win-arm\publish\TorrentStream.exe -DestinationPath win-arm.zip
& "dotnet" publish -c Release -r win-arm64 -p:PublishSingleFile=true --self-contained true
Compress-Archive -Path bin\Release\net7.0\win-arm64\publish\TorrentStream.exe -DestinationPath win-arm64.zip
& "dotnet" publish -c Release -r osx-x64 -p:PublishSingleFile=true --self-contained true
Compress-Archive -Path bin\Release\net7.0\osx-x64\publish\TorrentStream -DestinationPath osx-x64.zip
& "dotnet" publish -c Release -r osx-arm64 -p:PublishSingleFile=true --self-contained true
Compress-Archive -Path bin\Release\net7.0\osx-arm64\publish\TorrentStream -DestinationPath osx-arm64.zip
& "dotnet" publish -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained true
Compress-Archive -Path bin\Release\net7.0\linux-x64\publish\TorrentStream -DestinationPath linux-x64.zip
& "dotnet" publish -c Release -r linux-arm -p:PublishSingleFile=true --self-contained true
Compress-Archive -Path bin\Release\net7.0\linux-arm\publish\TorrentStream -DestinationPath linux-arm.zip
& "dotnet" publish -c Release -r linux-arm64 -p:PublishSingleFile=true --self-contained true
Compress-Archive -Path bin\Release\net7.0\linux-arm64\publish\TorrentStream -DestinationPath linux-arm64.zip
