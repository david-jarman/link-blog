---
id: ae27b751-4a8b-4943-bacd-176ccaac8a1d
title: 'New blog feature: paging'
short-title: blog-feature-paging
type: post
created: 2026-02-01T04:20:55.4425830+00:00
updated: 2026-02-01T04:20:55.4425830+00:00
link: ''
link-title: ''
tags:
- colophon
- dev-notes
---

When scrolling to the bottom of the home page, you now should see an "older posts" button which will take you to the next page of posts. This feature has been on my TODO list since day 1 and am happy it's finally implemented. It's much easier now that all posts are cached in memory in the blog, so I can just use LINQ statements to "Skip" and "Take" over the full post collection to emulate paging.
