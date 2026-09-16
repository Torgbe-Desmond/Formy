using Formify.Api.Data;
using Formify.Api.Exceptions;
using Formify.Api.Models;
using interfaces;
using Microsoft.EntityFrameworkCore;

namespace Formify.Api.Services;

public class FileContentService : IFileContentService
{
    private readonly AppDbContext _db;
    public FileContentService(AppDbContext db, IFileService fileService)
    {
        _db = db;
    }

    public async Task<Guid> UploadFileContentAsync(string content, CancellationToken ct = default)
    {
        var record = new FileContent { Content = content ?? string.Empty };
        _db.FileContents.Add(record);
        await _db.SaveChangesAsync(ct);
        return record.Id;
    }

    public async Task<string> DownloadFileContentAsync(Guid fileContentId, CancellationToken ct = default)
    {
        FileContent? record = await _db.FileContents.FirstOrDefaultAsync(fileContent => fileContent.Id == fileContentId);
        return record?.Content ?? string.Empty;
    }

    public async Task UpdateFileContentAsync(Guid contentId, string content, CancellationToken ct = default)
    {
        FileContent? fileContent = await _db.FileContents.FirstOrDefaultAsync(fileContent => fileContent.Id == contentId, ct);
        if (fileContent == null)
        {
            throw new NotFoundException($"File Content with id {contentId} not found.");
        }
        fileContent.Content = content ?? string.Empty;
        fileContent.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteFileContentAsync(Guid contentId, CancellationToken ct = default)
    {
        FileContent? fileContent = await _db.FileContents.FirstOrDefaultAsync(fileContent => fileContent.Id == contentId, ct);

        if (fileContent == null)
        {
            throw new NotFoundException($"File Content with id {contentId} not found.");
        }
        _db.FileContents.Remove(fileContent);
        await _db.SaveChangesAsync(ct);
    }

}
