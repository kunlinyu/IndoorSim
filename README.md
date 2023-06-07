# DATASTRUCTURE STANDARD
IndoorGML 1.1 https://docs.ogc.org/is/19-011r4/19-011r4.html

# UNITY3D VERSION
2022.1.9f1c1

# TRY IT
http://indoorsim.s3-website.cn-northwest-1.amazonaws.com.cn/V0.10.2/IndoorSim-WebGL-V0.10.2.4642fda/

Notice that this binary version is currently an internal tool of our company that collects sensitive data during operation. If you do not wish to disclose sensitive information, please refrain from using it.

# FOR DEVELOPER
0. Install Unity3D and VSCode

1. Install git lfs
curl -s https://packagecloud.io/install/repositories/github/git-lfs/script.deb.sh | sudo bash
sudo apt install git-lfs
git pull
git lfs pull

2. Install C# extension of VSCode
2.1 Dowload from marketplace
https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp -> Version History -> 1.24.4
2.2 Install from VSIX
2.3 close auto check
Preferences -> Settings -> Auto Check Update: false
2.4 useGlobalMono
"omnisharp.useGlobalMono":"always"

3. Install mono (Unbuntu 20.04)
https://www.mono-project.com/download/stable/
sudo apt install gnupg ca-certificates
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb https://download.mono-project.com/repo/ubuntu stable-bionic main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list
sudo apt update
sudo apt install mono-devel
