using LinkBlog.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkBlog.Web.Controllers;

[Route("admin")]
[Authorize(Policy = "Admin")]
public class AdminController : Controller
{
    private readonly IPostStore postStore;

    public AdminController(IPostStore postStore)
    {
        this.postStore = postStore;
    }

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> ArchivePostAsync(string id, CancellationToken ct)
    {
        bool success = await postStore.ArchivePostAsync(id, ct);
        if (!success)
        {
            return NotFound();
        }

        return Redirect("/admin?message=Post%20archived%20successfully");
    }
}
