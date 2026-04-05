---
id: f25dd03e-2770-41db-a9e6-cd904f6ea935
title: Adding the Trix editor to my Blazor SSR site
short-title: trix-is-for-adults
type: post
created: 2025-02-22T23:20:47.6295860+00:00
updated: 2025-02-22T23:20:47.6295860+00:00
link: https://trix-editor.org
link-title: 'Trix: Rich Editor'
tags:
- colophon
- trix
---

***Reinventing a blog website is so fun***I've been raw-dogging these posts thus far, manually typing HTML elements into a raw textarea in my admin page so I get something that looks halfway decent.

I decided it was time to upgrade my post editing experience. I was completely unaware of the term WYSIWYG (What You See Is What You Get), but figured that embedding a rich-text editor into a website must be a *very*solved problem at this point. So I asked ChatGPT (first 4o, then o3-mini, then o1-pro) for advice on what I should do for this blog. I looked at a few options first, but so many projects are licensed under GPL2 or have weird pricing models. I needed the most bare-bones simple text editor that's totally free to use. That's when I found Trix.

I'm really liking is so far. I was able to integrate it into my Blazor admin page in about 5 minutes, including time figuring out how to make sure the binding of the content gets set up correctly. [Here is the commit that shows the integration](https://github.com/david-jarman/link-blog/commit/d747aa58bc3e7c6ad95052d8d27e270a710bd631).

In my testing, it's covered 90% of my use cases for styling in my writing. I think my only grip so far is that you can only insert an h1 heading. Probably not a huge deal, and I may be able to play around with Trix a bit more and figure out how to add a custom toolbar action to insert an h2 heading instead. Or maybe I can just style the h1 in the posts class to be a bit smaller. We'll see.

I have lots of other fun things I want to do with this blog, so it may fall by the way side. I also added an Atom feed to the site today, served at [https://davidjarman.net/atom/all](https://davidjarman.net/atom/all).
