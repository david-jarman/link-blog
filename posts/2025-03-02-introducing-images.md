---
id: 48b94f51-4928-4d8a-9043-531fbd59b999
title: Images!
short-title: introducing-images
type: post
created: 2025-03-03T07:03:49.0242500+00:00
updated: 2025-03-03T07:03:49.0242500+00:00
link: ''
link-title: ''
tags:
- colophon
- aspire
- trix
- dev-notes
- images
---

Spent my free time adding image support to the blog today. I'm using Azure Storage accounts, as it's what I know, and I did not feel like diving into S3 buckets right now, although I probably should at some point.

Once again, .NET Aspire proved its usefulness. I kicked off the changes by adding the Azure Storage hosting and client integrations via Aspire, then added a Minimal API POST endpoint so I had a place to upload the images to. GitHub Copilot is very useful in these kinds of tasks. I can write a comment block describing what I want the endpoint to do, all the edge cases I think it should handle, then hit tab and get 90% of what I wanted. I go through and tweak the rest, then I'm testing almost immediately. 

The last piece of the puzzle was hooking up my new endpoint to the Trix editor. Fortunately, the Trix dev team provided a nice Javascript file to show how to hook up the event listeners and use an XHR object to post the image back to the server. Considering my lack of experience with Javascript, I was ecstatic to get this working in only a few iterations.

I edited my last post to add a picture from my bike ride, so I know it's working, but I better add one to this post too :)

[!\[\](https://linkblog.blob.core.windows.net/images/2025/03/03/07/01/49/2025012600044699778.jpeg)Being a goofball with my Instax camera](https://linkblog.blob.core.windows.net/images/2025/03/03/07/01/49/2025012600044699778.jpeg)
