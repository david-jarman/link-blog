using System.Runtime.CompilerServices;
using LinkBlog.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LinkBlog.Data;

public interface IGuestbookStore
{
    IAsyncEnumerable<GuestbookEntry> GetApprovedEntriesAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<GuestbookEntry> GetAllEntriesAsync(CancellationToken cancellationToken = default);

    Task<bool> CreateEntryAsync(GuestbookEntry entry, CancellationToken cancellationToken = default);

    Task<bool> ApproveEntryAsync(string id, CancellationToken cancellationToken = default);

    Task<bool> DeleteEntryAsync(string id, CancellationToken cancellationToken = default);

    Task<GuestbookEntry?> GetEntryByIdAsync(string id, CancellationToken cancellationToken = default);
}

public class GuestbookStoreDb : IGuestbookStore
{
    private readonly PostDbContext postDbContext;

    public GuestbookStoreDb(PostDbContext postDbContext)
    {
        this.postDbContext = postDbContext;
    }

    public async IAsyncEnumerable<GuestbookEntry> GetApprovedEntriesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var entries = this.postDbContext.GuestbookEntries
            .Where(e => e.IsApproved)
            .OrderByDescending(e => e.CreatedDate)
            .AsAsyncEnumerable();

        await foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return entry.ToGuestbookEntry();
        }
    }

    public async IAsyncEnumerable<GuestbookEntry> GetAllEntriesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var entries = this.postDbContext.GuestbookEntries
            .OrderByDescending(e => e.CreatedDate)
            .AsAsyncEnumerable();

        await foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return entry.ToGuestbookEntry();
        }
    }

    public async Task<bool> CreateEntryAsync(GuestbookEntry entry, CancellationToken cancellationToken = default)
    {
        var entity = entry.ToGuestbookEntryEntity();
        this.postDbContext.GuestbookEntries.Add(entity);
        await this.postDbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ApproveEntryAsync(string id, CancellationToken cancellationToken = default)
    {
        var entry = await this.postDbContext.GuestbookEntries
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entry == null)
        {
            return false;
        }

        entry.IsApproved = true;
        await this.postDbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteEntryAsync(string id, CancellationToken cancellationToken = default)
    {
        var entry = await this.postDbContext.GuestbookEntries
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entry == null)
        {
            return false;
        }

        this.postDbContext.GuestbookEntries.Remove(entry);
        await this.postDbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<GuestbookEntry?> GetEntryByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var entry = await this.postDbContext.GuestbookEntries
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return entry?.ToGuestbookEntry();
    }
}
