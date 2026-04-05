---
id: 236ab03c-fb92-48d8-b9e3-a3d030a8a8dc
title: Claude Code PowerShell tool
short-title: claude-code-pwsh-tool
type: post
created: 2026-03-26T17:52:44.9635990+00:00
updated: 2026-03-26T17:52:44.9635990+00:00
link: https://code.claude.com/docs/en/tools-reference#powershell-tool
link-title: Tools reference - PowerShell
tags:
- powershell
- claude
- ai
- windows
---

You can now opt-in to a new built-in PowerShell tool in Claude Code as of [v2.1.84](https://github.com/anthropics/claude-code/releases/tag/v2.1.84) by setting the CLAUDE\_CODE\_USE\_POWERSHELL\_TOOL environment variable.

Until this release, Claude Code on Windows would use Git Bash to run commands, which has caused some weird issues for me in the past, such as running the roslyn-language-server dotnet tool, because the entrypoint is a .cmd file which can't be run in a git bash shell.

Now, Claude Code will see an additional tool called "PowerShell"

```
  Built-in Tools

  - Read — Read files from the filesystem
  - Write — Write/create files
  - Edit — Make exact string replacements in files
  - Bash — Execute shell commands
  - Glob — Find files by pattern
  - Grep — Search file contents with regex
  - Agent — Launch specialized subagents
  - Skill — Invoke user-defined skills
  - ToolSearch — Fetch schemas for deferred tools
  - PowerShell — Execute PowerShell commands
```

It doesn't *replace* the Bash tool, it just adds PowerShell, so you may find that the agent defaults to using Bash for most things unless you ask it to use PowerShell explicitly. 

You can also add { "defaultShell": "powershell" } to your settings.json file to make !commands run in PowerShell. This means you can now run cmdlets directly. For example: ! Write-Host "Hello" now works.
[!\[\](https://linkblog.blob.core.windows.net/images/2026/03/26/17/52/17/image.png)hello world](https://linkblog.blob.core.windows.net/images/2026/03/26/17/52/17/image.png)
