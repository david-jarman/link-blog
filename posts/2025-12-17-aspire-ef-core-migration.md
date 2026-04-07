---
id: 73659e3e-2d64-4df0-99be-937ba99269e6
title: 'FYI: Automatically apply EF core migrations in Aspire'
short-title: aspire-ef-core-migration
type: post
created: 2025-12-18T05:58:27.4649320+00:00
updated: 2025-12-18T05:58:27.4649320+00:00
link: https://aspire.dev/integrations/databases/efcore/migrations/
link-title: Apply EF Core migrations in Aspire
tags:
- fyi
- aspire
- dotnet
---

If you are using Aspire and EF core to manage the schema of your database, this documentation is a great starting place to make the two work together.

The core concept is that you add a worker service that is dedicated to running migrations, creating the initial database, and seeding data to the database. Essentially any operation that should be performed before the actual web app starts up. I implemented this in the blog this evening with the help of Claude Code while I watched Twin Peaks.

One prerequisite was that I had to move my DbContext and Entity classes to a shared library so that the web app and worker services could both access them. I had actually tried to do this when I first created the blog but ran into issues and decided to simplify the design. I first told Claude to separate out the database entities, migrations, and contexts to a separate project. I was surprised how long this actually took. But in the end, something like 20+ files were touched, but all tests passed and the changes looked good.

Next, I started a new chat with Claude (/new command), and gave it the same link provided in this post.

> Follow the instructions at the following url to automatically apply migrations when running locally: https://aspire.dev/integrations/databases/efcore/migrations/

It then proceeded to one-shot the changes and everything just worked. Here are the full changes: [https://github.com/david-jarman/link-blog/pull/7](https://github.com/david-jarman/link-blog/pull/7)
