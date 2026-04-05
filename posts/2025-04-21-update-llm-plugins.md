---
id: 95b92313-8bac-40a8-8b82-b55b9f2f66f9
title: Update all llm plugins
short-title: update-llm-plugins
type: post
created: 2025-04-21T16:50:29.1677180+00:00
updated: 2025-04-21T16:50:29.1677180+00:00
link: ''
link-title: ''
tags:
- tools
- powershell
- ai
- llm
---

Quick one-liner to update all [llm](https://github.com/simonw/llm) plugins using [PowerShell](https://github.com/PowerShell/PowerShell):

```
llm plugins | ConvertFrom-Json | % { llm install -U $_.name }
```
