# -p:PublishTrimmed=true
& "dotnet" publish -c Release -r win-x86 -p:PublishSingleFile=true  --self-contained true
& "dotnet" publish -c Release -r win-x64 -p:PublishSingleFile=true  --self-contained true
& "dotnet" publish -c Release -r win-arm -p:PublishSingleFile=true  --self-contained true
& "dotnet" publish -c Release -r win-arm64 -p:PublishSingleFile=true  --self-contained true
& "dotnet" publish -c Release -r osx-x64 -p:PublishSingleFile=true  --self-contained true
& "dotnet" publish -c Release -r osx-arm64 -p:PublishSingleFile=true  --self-contained true
& "dotnet" publish -c Release -r linux-x64 -p:PublishSingleFile=true  --self-contained true
& "dotnet" publish -c Release -r linux-arm -p:PublishSingleFile=true  --self-contained true
& "dotnet" publish -c Release -r linux-arm64 -p:PublishSingleFile=true  --self-contained true