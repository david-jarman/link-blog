using LinkBlog.Data;
using Microsoft.AspNetCore.Mvc;

namespace LinkBlog.Web.Controllers;

public class KarmaController : Controller
{
    private readonly IPostStore postStore;

    public KarmaController(IPostStore postStore)
    {
        this.postStore = postStore;
    }

    [HttpPost("/api/karma/{id}")]
    public async Task<IActionResult> IncrementKarmaAsync(string id, CancellationToken ct)
    {
        var success = await this.postStore.IncrementKarmaAsync(id, ct);

        if (!success)
        {
            return NotFound();
        }

        return Redirect(Request.Headers.Referer.ToString() ?? "/");
    }
}
