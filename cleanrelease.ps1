Remove-Item -Path $env:USERPROFILE\Downloads\windows64.zip  -Force
Remove-Item -Path $env:USERPROFILE\Downloads\windowsarm64.zip  -Force
Remove-Item -Path $env:USERPROFILE\Downloads\macos64.zip  -Force
Remove-Item -Path $env:USERPROFILE\Downloads\macosarm64.zip -Force
Remove-Item -Path $env:USERPROFILE\Downloads\linux64.zip -Force
Remove-Item -Path $env:USERPROFILE\Downloads\linuxarm64.zip -Force

Remove-Item -Path windows64.exe -Force
Remove-Item -Path windowsarm64.exe -Force
Remove-Item -Path linux64 -Force
Remove-Item -Path linuxarm64 -Force
Remove-Item -Path macos64 -Force
Remove-Item -Path macosarm64 -Force
Remove-Item -Path deploy -Recurse -Force