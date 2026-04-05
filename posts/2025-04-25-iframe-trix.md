---
id: ac171c34-84b3-41b6-a071-20025fa49230
title: Embedding an iframe using Trix
short-title: iframe-trix
type: post
created: 2025-04-25T14:54:15.9059840+00:00
updated: 2025-04-25T15:06:45.0281750+00:00
link: ''
link-title: ''
tags:
- colophon
- trix
- dev-notes
---

In my [previous post about riding to Cle Elum](https://davidjarman.net/archive/2025/04/23/big-ride-plans), I ran into issues with [Trix](https://github.com/basecamp/trix) removing the iframe I was trying to add to show my cycling route on [Ride With GPS](https://ridewithgps.com/). [Here is the commit with the fixes](https://github.com/david-jarman/link-blog/commit/defa8ad7ae396d6795f7dacf2a1121ee777a1b57).

There were multiple issues that needed to be solved:

1. Add a custom button that would allow me to put arbitrary HTML into a post (supported feature from Trix)
2. Modify the DOMPurify config in Trix to allow iframes (also supported feature)
3. Modify the actual trix.js code to allow iframes (not supported feature, requires forking).

[This issue on GitHub](https://github.com/basecamp/trix/issues/1178) helped me get there, but ultimately I needed to find my own path forward that made sense for this site. Claude code also gave me a great start by writing the majority of the [trix-extensions.js](https://github.com/david-jarman/link-blog/blob/main/src/LinkBlog.Web/wwwroot/js/trix-extensions.js) file, but I had to do a fair amount of manual clean up to get it to the finish line. But it's so nice to have the AI do the boilerplate to begin with, which gives me a great starting point.

The worst part about this fix was having to patch the trix code directly instead of being able to extend it. But since I [vendor the trix code](https://davidjarman.net/archive/2025/02/24/vendoring-essay) in my repo, it was very easy to do. I actually modified the minified trix.js file directly by ctrl+f "iframe" to remove it from the disallow list. If I need to update trix.js in the future, I'll have to remember to re-apply this fix, so this is definitely a tradeoff, but at least it was an easy way to validate the approach.
