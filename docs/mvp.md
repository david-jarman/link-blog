# MVP

The MVP of the link blog will be the set of features that should be shipped during the first deployment.

## Features

1. Display the latest 10 posts
2. Posts have a set of tags
3. Clicking a tag shows you all posts that contain that tag

## Shortcuts

- Posts can be stored as JSON in the codebase for now. Add a database later.
- To add a new post, check it into source, then deploy new instance of web app.
- No logging backend for now, just log to console.
- Don't worry about https for now, http (port 80) is fine.

## Post metadata

What data should a post contain?

- Id (permanent reference)
- Link (optional)
- Link Title
- Contents (optional, if Link is specified)
- Title
- Date
- Tags

## Roadmap
- HTTPS via Let's Encrypt
- Search
- Store posts in a database
- API to create posts
  - Authentication
- RSS/Atom feed
  - All Posts feed
  - Feed per tag
- Dark mode
- Permalinks for posts
- Hosting
- CI/CD
- Analytics