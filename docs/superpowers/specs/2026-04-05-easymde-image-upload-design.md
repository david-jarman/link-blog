# EasyMDE Image Upload Design

**Date:** 2026-04-05

## Overview

Add image upload support to the EasyMDE post editor in the admin page. The existing Azure Blob Storage upload infrastructure is already in place (`POST /api/upload`). This work connects EasyMDE's built-in upload UI to that endpoint by aligning the response format.

## Controller Change

**File:** `src/LinkBlog.Web/Controllers/UploadImageController.cs`

Replace the success return from:
```csharp
return Created(blobClient.Uri.AbsoluteUri, null);
```
To:
```csharp
return Ok(new { data = new { filePath = blobClient.Uri.AbsoluteUri } });
```

EasyMDE's `imageUploadEndpoint` expects HTTP 200 with JSON body `{"data": {"filePath": "<url>"}}`. All error paths (400, 409, 500) remain unchanged.

## EasyMDE Configuration Change

**File:** `src/LinkBlog.Web/Components/Pages/Admin/AdminHome.razor`

Add to the `new EasyMDE({...})` initializer:

```js
imageUploadEndpoint: "/api/upload",
imagePathAbsolute: true,
imageAccept: "image/png, image/jpeg, image/gif, image/webp",
```

`imagePathAbsolute: true` tells EasyMDE to use the returned URL as-is rather than treating it as a relative path. EasyMDE's default toolbar already includes an image upload button — no toolbar customization needed.

## Out of Scope

- File size limits (EasyMDE default: 2 MB; server-side limits handled by existing `IImageConverter`)
- UI styling of the upload progress state
- Changes to error handling logic
