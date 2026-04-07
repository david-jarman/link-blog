---
id: 8c24301a-7686-4abd-97b5-c54201a581f1
title: 'FYI: Tracking down transitive dependencies in .NET'
short-title: dotnet-nuget-why
type: post
created: 2025-04-18T18:55:09.7893540+00:00
updated: 2025-04-18T18:55:09.7893540+00:00
link: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-why
link-title: dotnet nuget why - command reference
tags:
- fyi
- tools
- dependencies
- dotnet
---

I just found that there is a new(ish) command for figuring out where a transitive dependency comes from in your dotnet project (starting with dotnet 8.0.4xx)

```
dotnet nuget why <PROJECT|SOLUTION> <PACKAGE>
```

If you have a dependency in your project that has a vulnerability, you can use this to figure out which package is bringing it in. For example, [System.Net.Http 4.3.0 has a high severity vulnerability](https://github.com/advisories/GHSA-7jgj-8wvc-jh57). I've found instances where this package is brought into my projects by other packages. It's very handy to be able to trace it with a built-in tool. Before this was available, I would use the d[otnet-depends tool](https://github.com/bjorkstromm/depends), which is a great tool, but a little clunkier than I'd like, and doesn't seem to [support central package management](https://github.com/bjorkstromm/depends/issues/32).
