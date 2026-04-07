---
id: a03b9d1e-b45c-416c-b26b-3fdc24c9f240
title: Get notifications from Claude Code on Windows with WSL
short-title: claude-code-notifications-wsl
type: post
created: 2026-01-09T23:31:51.4534450+00:00
updated: 2026-01-09T23:36:56.4094330+00:00
link: https://gist.github.com/david-jarman/27d6eec0ec0114f545d5dff84152a9ed
link-title: My ~/.claude/settings.json with full solution
tags:
- wsl
- claude
- ai
- windows
---

I've been looking for a way to get notified when Claude Code needs my input or is finished. Big shout out to u/Ok-Engineering2612 on Reddit for this post:  [WSL Toast Notifications with Hooks in Claude Code : r/ClaudeAI](https://www.reddit.com/r/ClaudeAI/comments/1m2qscz/wsl_toast_notifications_with_hooks_in_claude_code/). I had been trying to do the same thing with BurntToast but I forget the way WSL interops with Windows.

The settings from the Reddit thread did need a little tweaking ($PAYLOAD is no longer supported and now Claude Code sends the JSON structure via stdin). Here's my change to the command:

```
 "command": "input=$(cat) && powershell.exe -NoProfile -Command \"Import-Module BurntToast; New-BurntToastNotification -Text 'Claude Code Notification', '$(echo \"$input\" | jq -r '.message')'\""
```

Here is the full documentation for hooks: [Hooks reference](https://code.claude.com/docs/en/hooks#notification-input)
