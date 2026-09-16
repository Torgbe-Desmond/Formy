using System.Text;
using Extensions;
using Formify.Api.Data;
using Formify.Api.Dtos;
using Formify.Api.Exceptions;
using Formify.Api.Models;
using interfaces;
using Microsoft.EntityFrameworkCore;

namespace Formify.Api.Services;

public class FileService : IFileService
{
    private readonly AppDbContext _db;
    public FileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AppFile?> GetFileById(Guid fileId, CancellationToken ct = default)
    {
        AppFile? file = await _db.AppFiles
           .Include(f => f.Metadata)
           .FirstOrDefaultAsync(f => f.Id == fileId, ct);
        if (file == null) throw new NotFoundException($"File with id {fileId} was not found.");
        return file;
    }


    public async Task<Guid> UploadAsync(string content, CancellationToken ct = default)
    {
        var record = new FileContent { Content = content ?? string.Empty };
        _db.FileContents.Add(record);
        await _db.SaveChangesAsync(ct);
        return record.Id;
    }


    public async Task<AppFileDto> AddAsync(Guid folderId, CreateFileRequest request, CancellationToken ct = default)
    {
        var content = request.Content ?? string.Empty;
        Guid contentId = await this.UploadAsync(content, ct);

        AppFile file = new AppFile
        {
            Name = request.Name,
            FolderId = folderId,
            ContentId = contentId,
            SizeBytes = Encoding.UTF8.GetByteCount(content),
        };

        if (request.Metadata is not null)
        {
            foreach (var (key, value) in request.Metadata)
                file.Metadata.Add(new AppFileMetadata { Key = key, Value = value ?? string.Empty });
        }

        _db.AppFiles.Add(file);
        await _db.SaveChangesAsync(ct);

        return file.ToDto();
    }

    public async Task<List<AppFile>> GetFileListByFolderId(Guid folderId, CancellationToken ct = default)
    {

        List<AppFile> files = await _db.AppFiles
            .Include(f => f.Metadata)
            .Where(f => f.FolderId == folderId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

        return files;
    }


    public async Task DeleteFileAsync(Guid fileId, CancellationToken ct = default)
    {

        AppFile? appFile = await _db.AppFiles.FirstOrDefaultAsync(file => file.Id == fileId);
        if (appFile == null) throw new NotFoundException($"File with id {fileId} not found.");
        _db.AppFiles.Remove(appFile);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RenameFileAsync(Guid fileId, string newName, CancellationToken ct = default)
    {
        AppFile? appFile = await _db.AppFiles.FirstOrDefaultAsync(file => file.Id == fileId);
        if (appFile == null) throw new NotFoundException($"File with id {fileId} not found.");
        appFile.Name = newName;
        await _db.SaveChangesAsync(ct);
    }


    public async Task<AppFile> UpdateFileAsync(AppFile file, CancellationToken ct = default)
    {
        file.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return file;
    }

}
