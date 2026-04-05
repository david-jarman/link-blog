# Markdown-Based Post Storage Design

**Date:** 2026-04-04  
**Status:** Approved

## Summary

Replace the PostgreSQL database dependency with markdown files stored in Azure Blob Storage. Posts are authored via a browser-based markdown editor in the admin page, persisted to Blob, and backed up to git via a scheduled GitHub Action. Markdig converts markdown bodies to HTML at load time.

---

## Architecture & Data Flow

### Read Path

`IPostStore` is unchanged — all Blazor pages continue to work without modification. The implementation underneath changes:

- On startup, `MarkdownPostDataAccess` reads all `.md` files from Azure Blob Storage, parses YAML frontmatter and markdown body, renders markdown → HTML via Markdig, and loads posts into `CachedPostStore`.
- A background polling service checks Azure Blob for changes (using ETags or last-modified timestamps) every N minutes and refreshes the cache when files have changed. This means new/edited posts go live without an app redeploy.

### Write Path

- Admin page saves a post → serialized to markdown with YAML frontmatter → written to Azure Blob Storage via `IPostStore.CreatePostAsync` / `UpdatePostAsync`.
- `CachedPostStore` cache is invalidated immediately after the write so the post is live without waiting for the next poll.

### Git Backup

A scheduled GitHub Action (e.g., nightly) downloads all `.md` files from the Blob container and commits any changes to the git repository. Git is a backup, not the primary runtime source. A short lag (up to ~24 hours) between authoring and git backup is acceptable for a personal blog.

### What is Removed

- PostgreSQL database and all EF Core infrastructure
- `LinkBlog.MigrationService` project
- Aspire Postgres resource from `AppHost`
- Trix WYSIWYG editor and draft manager

---

## Markdown File Format

**File naming convention:** `{YYYY-MM-DD}-{short-title}.md`  
Example: `2025-03-15-my-post-title.md`

```markdown
---
id: abc-123-def-456
title: My Post Title
short-title: my-post-title
type: post
created: 2025-03-15T10:00:00-08:00
updated: 2025-03-15T10:00:00-08:00
link: https://example.com/some-article
link-title: External Article Title
tags: [tag1, tag2, tag3]
archived: false
---

Post body content in markdown...
```

**Field notes:**
- `link` and `link-title` are optional — omit entirely for non-link posts.
- `archived: false` can be omitted (default false); `archived: true` marks a post as archived. Archiving updates the frontmatter in the Blob file.
- `type` defaults to `"post"`. Reserved for future content types (e.g., `note`, `fyi`). `IPostStore` queries do not filter by type yet.
- `id` is preserved from existing DB records during migration; new posts get a freshly generated GUID.

---

## Component Changes

### Removed

- `LinkBlog.MigrationService` project (deleted entirely)
- `LinkBlog.Data`: `PostDbContext`, `PostEntity`, `TagEntity`, all EF Core migrations, `PostDataAccess` (EF Core implementation — `IPostDataAccess` interface is kept)
- Packages: `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.*` from `LinkBlog.Data` and `LinkBlog.Web`
- Aspire Postgres hosting from `AppHost`
- Trix JS/CSS and draft manager JS/CSS

### Added to `LinkBlog.Data`

- `MarkdownPostDataAccess` — reads/writes `.md` files to/from Azure Blob Storage, parses YAML frontmatter, renders markdown via Markdig. Implements `IPostDataAccess`.
- `YamlDotNet` package — YAML frontmatter parsing.
- `Markdig` package — markdown → HTML rendering.
- Background polling service — replaces `PostCacheRefreshService`, watches Blob ETags instead of polling DB.

### Changed

- `Post` model in `LinkBlog.Abstractions` gains a `Type` property (string, defaults to `"post"`).
- `CachedPostStore` unchanged in structure; its `IPostDataAccess` dependency now resolves to `MarkdownPostDataAccess`.
- `AppHost`: Postgres resource and MigrationService references removed.
- Admin page: Trix replaced with [EasyMDE](https://github.com/Ionaru/easy-markdown-editor) (MIT-licensed, self-hosted, no external CDN). EasyMDE has built-in local storage autosave, replacing the Trix draft manager.

---

## Admin Page

The admin page retains its current structure (create/edit/archive posts). Changes:

- **Editor:** Trix replaced with EasyMDE, a self-hosted markdown editor loaded from local JS/CSS files. JavaScript is acceptable on admin pages per project guidelines.
- **Write path:** On save, the post is serialized to the markdown format and written to Azure Blob via `IPostStore`. Cache is invalidated immediately.
- **Archive:** Updates `archived: true` in the post's frontmatter, re-saves to Blob, invalidates cache.
- **Removed:** Trix JS/CSS, draft manager JS/CSS, `@using Microsoft.EntityFrameworkCore`.

---

## Markdig Configuration

The Markdig pipeline is configured with:
- Standard CommonMark rendering
- Markdig advanced extensions (tables, task lists, etc.)
- Raw HTML **disabled** — posts should not contain raw HTML

### RideWithGPS Embeds (Needs Research)

A small number of existing posts contain RideWithGPS `<iframe>` embeds. The intent is to write a custom Markdig extension that auto-detects a standalone RideWithGPS URL on its own line and renders it as the appropriate embed iframe — keeping markdown clean with no raw HTML required.

**Proposed author syntax:**
```markdown
https://ridewithgps.com/routes/12345
```

This approach could be extended to other embed providers (YouTube, Strava, etc.) using the same pattern.

**Status: Needs research.** Implementation requires investigation of the Markdig extension API before work begins. This is a known gap and should be treated as a separate task after the core migration is complete.

---

## Migration Plan

A one-time migration tool (`tools/LinkBlog.Migration` console app) handles moving existing posts from Postgres to markdown files.

**Steps:**
1. Connect to existing Postgres DB via EF Core.
2. Read all posts (including archived).
3. Convert each post's HTML `Contents` to markdown using `ReverseMarkdown` (.NET HTML→markdown library).
4. Detect any RideWithGPS `<iframe>` embeds — replace with the bare RideWithGPS URL on its own line and log a warning for manual review.
5. Log a warning for any other posts containing HTML that `ReverseMarkdown` could not cleanly convert.
6. Write each post as a `.md` file to a local output directory, preserving original `id`, `created`, and `updated` values.

**Deployment sequence:**
1. Run migration tool → review output → commit markdown files to git.
2. Upload markdown files to Azure Blob container.
3. Deploy new app version (Postgres removed, Blob-backed).
4. Verify all posts render correctly.
5. Decommission Postgres add-on on Heroku.

---

## Packages

| Package | Action |
|---|---|
| `Markdig` | Add to `LinkBlog.Data` |
| `YamlDotNet` | Add to `LinkBlog.Data` |
| `ReverseMarkdown` | Add to `tools/LinkBlog.Migration` only |
| `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` | Remove from `LinkBlog.Data`, `LinkBlog.Web` |
| `Microsoft.EntityFrameworkCore.Design` | Remove from `LinkBlog.Data`, `LinkBlog.Web` |
| `Aspire.Hosting.PostgreSQL` | Remove from `LinkBlog.AppHost` |
