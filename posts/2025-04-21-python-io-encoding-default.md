---
id: 3f961fdc-a975-4346-a835-0a3df36115d4
title: Set default pipe/redirect encoding in Python
short-title: python-io-encoding-default
type: post
created: 2025-04-21T18:11:59.0421040+00:00
updated: 2025-04-21T18:12:42.6609180+00:00
link: https://stackoverflow.com/a/27066059
link-title: '[via] Changing default encoding of Python? - StackOverflow'
tags:
- tools
- powershell
- ai
- llm
---

I ran into an issue using [llm](https://github.com/simonw/llm) today where I was unable to save a response to a file using a pipe

```
llm llm logs -n 1 | Out-File response.txt
```

This would give me the error "UnicodeEncodeError: 'charmap' codec can't encode character '\u2192' in position 2831: character maps to &lt;undefined&gt;"

If you set the "PYTHONIOENCODING" environment variable to "utf8", it will fix the issue. This is because Python's default encoding is ASCII. Since the last response I got back from the model contained a non-ASCII character, this error was thrown.

So now, in my [PowerShell profile](https://gist.github.com/david-jarman/bca0fe36ba699885c4156e8aeed8bbac#file-microsoft-powershell_profile-ps1-L43), I've added a line to set the default to utf8, which fixes the issue.

```
$env:PYTHONIOENCODING = 'utf8'
```
