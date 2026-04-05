---
id: 60247833-9dc0-4962-907f-24bd60bcea99
title: Edit - a Windows-native CLI text editor
short-title: edit-windows-cli
type: post
created: 2025-05-19T19:18:36.6127320+00:00
updated: 2025-05-19T19:18:36.6127320+00:00
link: https://github.com/microsoft/edit
link-title: GitHub - Edit
tags:
- cli
- tools
- microsoft
---

It's 2025, and I'm excited about a new CLI text editor. When I need a text editor in a shell, I always reach for vim, but I don't love it. It's there when I need it and serves its purpose, but I've never gotten over the weird key binding knowledge required just to exit and save a file.

What I like about Edit is that it's built as a TUI (Terminal User Interface) which means you can use your mouse (point and click), you can use ctrl+a to select all text, you can use ctrl+c and ctrl+v for copy paste, and it's just way more intuitive to use.

**Getting it to run**

The tool was just released, so as expected, there are some quirks. First and foremost, [Windows Defenders think the pre-built binary the released is a virus](https://github.com/microsoft/edit/issues/42)! In order to actually play with it, I had to build it from source using the rust tool chain.

Here's what I had to do:

Install the C++ toolchain using Visual Studio Installer.
Restart my shell, to update paths.
[Install the rust toolchain](https://www.rust-lang.org/tools/install)
git clone https://github.com/microsoft/edit.git
rustup install nightly
rustup default nightly-x86\_64-pc-windows-msvc (to set the nightly toolchain as the default)
rustup component add rust-src --toolchain nightly-x86\_64-pc-windows-msvc (no idea why I had to do this, but rustup said I had to)
cargo build --config .cargo/release.toml --release (compile and linking step)
cp .\target\release\edit.exe D:\tools\ (D:\tools is where I store my adhoc tools that I build or maintain)

A few of these steps were documented on the Edit README.md file, but several were missing.
