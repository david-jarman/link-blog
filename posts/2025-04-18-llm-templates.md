---
id: a0c08830-b04a-4869-a618-869f1b974f5e
title: LLM templates
short-title: llm-templates
type: post
created: 2025-04-18T17:48:08.0500370+00:00
updated: 2025-04-18T17:48:08.0500370+00:00
link: https://github.com/david-jarman/llm-templates
link-title: 'david-jarman/llm-templates: LLM templates to share'
tags:
- tools
- ai
- files-to-prompt
- llm
---

Simon Willison's LLM tool [now supports sharing and re-using prompt templates](https://simonwillison.net/2025/Apr/7/long-context-llm/#publishing-sharing-and-reusing-templates). This means you can create yaml prompt templates in GitHub and then consume them from anywhere using the syntax llm -t gh:{username}/{template-name}.

I have created my own repo where I will be uploading my prompt templates that I use. My most recent template that I've been getting value out of is "[update-docs](https://github.com/david-jarman/llm-templates/blob/main/update-docs.yaml)". I use this prompt/model combination to update documentation in my codebases after I've refactored code or added new functionality. The setup is that I use "files-to-prompt" to build the context of the codebase, including samples, then add a single markdown document that I want to be updated at the end. I've found that asking the AI to do too many things at once ends up with really bad results. I've also been playing around with different models. I haven't come to a conclusion on which is the absolute best for updating documentation, but so far o4-mini has given me better vibes than GPT 4.1.

Here is the one-liner command I use to update each document:

```
files-to-prompt -c -e cs -e md -e csproj --ignore "bin*" --ignore "obj*" /path/to/code /path/to/samples /path/to/doc.md | llm -t gh:david-jarman/update-docs
```

You can override the model in the llm call using "-m &lt;model&gt;"

```
llm -t gh:david-jarman/update-docs -m gemini-2.5-pro-exp-03-25
```

The next thing I'd like to tackle is creating a fragment provider for this scenario so I don't have to add so many paths to files-to-prompt. It's a bit clunky and I think it would be more elegant to just have a fragment provider that knows about my codebase structure and can bring in the samples and code without me needing to specify it each time.
