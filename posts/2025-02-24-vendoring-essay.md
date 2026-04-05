---
id: 56dbc42f-7aeb-4f29-988b-30c61d1ac485
title: '"Vendoring"'
short-title: vendoring-essay
type: post
created: 2025-02-24T20:11:38.5057400+00:00
updated: 2025-02-24T20:11:38.5057400+00:00
link: https://htmx.org/essays/vendoring/
link-title: Vendoring
tags:
- programming
- dependencies
---

> “Vendoring” software is a technique where you copy the source of another project directly into your own project.

I love this idea, so many times you need to debug into your dependencies to figure out an issue or why things aren't working as you expected.

I mainly develop in C# and .NET and am fortunate to work at Microsoft, so I've done this many times in the past where I grab the source for a dependency, copy it into my code, update the references, and start debugging. This is always a temporary step. I don't actually check in the dependent code. I don't do it that often anymore, as you can enable debug options to disable "just my code" and if your dependencies publish symbols (most do in my experience), you can just F11 and step into library code from Nuget packages.

In the future, I may take up the vendoring approach for web frontends, where licenses allow. If nothing else, it gives me peace of mind that the dependency won't just disappear from the CDN.
