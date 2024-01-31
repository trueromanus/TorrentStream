```shell
sudo flatpak install flathub org.freedesktop.Platform//23.08 org.freedesktop.Sdk//23.08
sudo flatpak install org.freedesktop.Sdk.Extension.dotnet8
flatpak-builder --force-clean build tv.anilibria.torrentstream.yml

```
