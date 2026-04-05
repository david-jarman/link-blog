---
id: 69b16f23-94d6-486d-b2d4-3f4c6696ccf1
title: Claude Code Initial Impressions
short-title: claude-code-initial-impressions
type: post
created: 2025-02-24T21:36:53.2884550+00:00
updated: 2025-02-24T22:23:53.4111610+00:00
link: https://www.anthropic.com/news/claude-3-7-sonnet
link-title: Claude Code Announcement
tags:
- llms
- claude
- ai
---

Claude announced a new hybrid reasoning model today. That's a great idea to have a singular model for both reasoning and quick responses. 

What I'm more interested in is their new Claude Code tool. It's an interactive CLI that is similar to GitHub Copilot or Cursor, but only runs in your terminal as of now. Here is the link for setting it up: [https://docs.anthropic.com/en/docs/agents-and-tools/claude-code/overview](https://docs.anthropic.com/en/docs/agents-and-tools/claude-code/overview)

I was hoping that this tool would just use my existing Claude plan, but no, of course you actually pay for the tokens it uses. I'm sure this was a very conscious decision, as this tool uses A LOT of tokens right now. I mean, it's breathtaking. The first thing I did was load it up on my link blog codebase, and ran the /init command to generate a readme file for the codebase. I immediately ran the /cost command to see how much that operation costed. Thirty cents. That may not sound like much, but for as small as my codebase is, I was expecting that to only be a few cents. I then gave it a very specific task to add validation to my admin post authoring form. I gave it a fair bit of instruction, as the docs recommends treating the tool like a software engineer that you would delegate a task to. So I gave it hints as to how to find validation rules and all that. I then sent it off. It ran for something like 2 minutes making the change. It prompted me for permission to perform tool actions (e.g. run some bash commands, run build, etc). After a total of 10 minutes of use, I was up to $1.50 in spend, the code did not build, and I realized that the [tool call to build the code was broken](https://github.com/anthropics/claude-code/issues/26). ***Edit: It turns out powershell is not officially supported yet. You must use bash or zsh to launch claude.***

I'm still excited about this tool and will keep playing around with it. I'll probably have to reload my anthropic wallet with more credits soon as it is expensive, but so far it seems like a really cool concept, and I hope they keep improving it and driving down the cost.
