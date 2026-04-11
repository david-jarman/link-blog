using LinkBlog.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace LinkBlog.Web.Controllers;

[Route("admin")]
[Authorize(Policy = "Admin")]
public class AdminController : Controller
{
    private readonly IPostStore postStore;
    private readonly IOutputCacheStore outputCacheStore;

    public AdminController(IPostStore postStore, IOutputCacheStore outputCacheStore)
    {
        this.postStore = postStore;
        this.outputCacheStore = outputCacheStore;
    }

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> ArchivePostAsync(string id, CancellationToken ct)
    {
        bool success = await postStore.ArchivePostAsync(id, ct);
        if (!success)
        {
            return NotFound();
        }

        await outputCacheStore.EvictByTagAsync("posts", ct);
        return Redirect("/admin?message=Post%20archived%20successfully");
    }
}