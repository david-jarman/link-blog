---
id: 0f0e58da-9022-486b-8f7f-22f9d870e0b5
title: Setting up a dotnet dev environment with WSL and Ubuntu 24.04
short-title: dotnet-dev-wsl-ubuntu
type: post
created: 2026-01-05T21:49:52.5319310+00:00
updated: 2026-01-06T17:32:13.2976590+00:00
link: ''
link-title: ''
tags:
- programming
- ubuntu
- wsl
- dotnet
- linux
---

I just knew 2026 was going to start off this way... A complicated setup to do something that should be simple.

Ok, so you want to do some .NET dev work with [WSL](https://learn.microsoft.com/en-us/windows/wsl/about)? Better not choose Ubuntu 24.04 as your distro. I guess this version doesn't ship with some important packages that are needed to make some interop work between Windows and Linux. Anyways, let's get to the steps!

**Configure Git for authentication with Azure DevOps**

1. Make sure you have installed Git For Windows on your host machine. Your git installed in Linux will need to use the credential manager in Windows.
2. Configure the credential helper: git config --global credential.helper "/mnt/c/Program\ Files/Git/mingw64/bin/git-credential-manager.exe"
3. Configure using HTTP: git config --global credential.https://dev.azure.com.useHttpPath true
4. Clone a repo hosted in Azure DevOps

**Install Dotnet**

1. Install dotnet: wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh && chmod +x ./dotnet-install.sh && ./dotnet-install.sh --jsonfile global.json
2. Edit ~/.bashrc to add dotnet to the PATH

```
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
```

**Configure dotnet for authentication with Azure DevOps**

1. Install [Artifacts CredProvider](https://github.com/microsoft/artifacts-credprovider): wget -qO- https://aka.ms/install-artifacts-credprovider.sh | bash
2. Set environment variable in ~/.bashrc to force a dialog to popup for authentication so you don't end up using Device Flow (which some tenants, like mine, disallow)

```
export NUGET_CREDENTIALPROVIDER_FORCE_CANSHOWDIALOG_TO=true
```

**Install packages that allow the browser to be opened from MSAL**This part is crucial. When you run dotnet restore --interactive, MSAL will want to open your system's browser so you can use OAuth to sign in and provide credentials for your Azure DevOps instance. Install these packages, which no longer ship with Ubuntu:

1. apt update
2. apt install xdg-utils
3. apt install wslu

[wslu](https://github.com/wslutilities/wslu) is a discontinued project that consisted of utilities for WSL and xdg-open is a program that will open the system's browser.

**Edit: If WSL interop stops working**You may need to add a WSL interop config ([source](https://github.com/microsoft/WSL/issues/8843#issuecomment-1337127239))

```
sudo vim /usr/lib/binfmt.d/WSLInterop.conf
Add this line:
:WSLInterop:M::MZ::/init:PF
```

Then, restart systemd-binfmt

```
sudo systemctl restart systemd-binfmt 
```

**Final step**

```
dotnet restore --interactive
```

This should open your browser and you can sign in and restore packages! This only took me a full morning to figure out :(
[!\[\](https://linkblog.blob.core.windows.net/images/2026/01/05/21/49/33/elmo.gif)me in 2026](https://linkblog.blob.core.windows.net/images/2026/01/05/21/49/33/elmo.gif)
[Microsoft Loves Linux - Microsoft Windows Server Blog](https://www.microsoft.com/en-us/windows-server/blog/2015/05/06/microsoft-loves-linux)
