---
id: dad0edef-7156-4ee7-9648-df8ea3f3a7d0
title: 'FYI: dotnet-outdated'
short-title: dotnet-outdated-tool
type: post
created: 2025-12-17T04:08:04.2984970+00:00
updated: 2025-12-17T04:08:04.2984970+00:00
link: https://github.com/dotnet-outdated/dotnet-outdated
link-title: 'GitHub: Dotnet Outdated'
tags:
- fyi
- tools
- dependencies
- dotnet
---

I hate having to update package dependencies in projects. Fortunately there is a handy dotnet tool that will report and update packages that are out of date. I used this to update all the packages in the link-blog source code this evening and was pleasantly surprised it just worked. Only issue I found was that because I create a msbuild property to store the OpenTelemetry version (there are three OTel packages with the same version), the tool updated the PackageVersions directly instead of just updating the property. Not a big deal, and I would have been shocked if it was able to handle a corner case like that.

Now I need to see if I can get this to run as a daily CI task.
