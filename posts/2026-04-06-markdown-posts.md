---
id: 0cc2d793-a94d-4b36-b9bd-6518c0c7b58a
title: Removing Postgres and storing posts as Markdown files
short-title: markdown-posts
type: post
created: 2026-04-07T05:17:21.5690337+00:00
updated: 2026-04-07T05:17:21.5690337+00:00
tags:
- colophon
- dev-notes
- claude-code
- superpowers
---

Now that Heroku is on the deprecation path, I've been looking to move this blog to another provider. One thing I quickly realized is that managed SQL servers are usually not this cheap. I'm currently paying $5 a month for the cheapest tier but I haven't seen another provider with that good of a price point. This got me thinking, why use SQL at all for this simple site? Why not just store posts as markdown with YAML frontmatter?

I've been wanting to try out the acclaimed [superpowers](https://github.com/obra/superpowers) plugin for claude code, and this was just the right feature to use it on. Did a long brainstorm session with the agent to flesh out the idea, then using the subagent-driven development method, had Claude implement it. A bunch of subagents got to work and had the whole thing working end-to-end pretty quickly. One thing I forgot to flesh out was the replacement of my custom image-upload JS from my old WYSIWYG editor, trix. We replaced it with [EasyMDE](https://github.com/ionaru/easy-markdown-editor) and all I had to do was point it at my image upload endpoint and change a bit of formatting on the server side.

I'm very impressed with superpowers. I paired it with Simon Willison's [Rodney](https://github.com/simonw/rodney) CLI so the agents could test out their changes and "see" the actual impact of their code in a headless browser. I now have much *less* code running on the blog and no Postgres dependency, so I should be free to switch to another provider, such as [fly.io](https://fly.io) and hopefully halve my hosting cost.