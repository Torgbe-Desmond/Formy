using Formify.Api.Data;
using Formify.Api.Dtos;
using Formify.Api.Exceptions;
using Formify.Api.Models;
using interfaces;
using Microsoft.EntityFrameworkCore;

namespace Formify.Api.Services;



/// <summary>
/// Recursively builds a breadcrumb trail from any entity back to the root
/// (project → folder → file). Direct port of Node's breadcrumb.service.js.
/// </summary>
public class FolderService : IFolderSerivce
{
    private readonly AppDbContext _db;
    private readonly ISchemaService _schemaService;
    public FolderService(AppDbContext db, ISchemaService schemaService)
    {
        _db = db;
        _schemaService = schemaService;
    }
    public async Task<Folder?> GetFolderById(Guid folderId, CancellationToken ct = default)
    {
        try
        {
            Folder? folder = await _db.Folders.FirstOrDefaultAsync(f => f.Id == folderId, ct)
          ?? throw new NotFoundException("Folder not found");
            return folder;
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<List<Folder>> GetFolderListByProjectId(Guid projectId, CancellationToken ct = default)
    {

        List<Folder> folders = await _db.Folders
            .Where(f => f.ProjectId == projectId)
            .Include(f => f.Files)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

        return folders;
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


    public async Task<Folder> AddFolderAsync(Guid projectId, FolderRequest request, CancellationToken ct = default)
    {
        Folder folder = new Folder { Name = request.Name.Trim(), ProjectId = projectId };
        _db.Folders.Add(folder);

        // On every folder creation there will be an entry schema created for it.
        await _schemaService.AddEntrySchemaAsync(folder.Id);
        await _db.SaveChangesAsync(ct);
        return folder;
    }

    public async Task<Folder> UpdateFolderAsync(Guid folderId, FolderRequest request, CancellationToken ct = default)
    {
        Folder? folder = await this.GetFolderById(folderId, ct);

        if (folder == null) throw new NotFoundException($"Folder with id {folderId} was not found.");

        folder.Name = request.Name.Trim();
        folder.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return folder;
    }

    public async Task DeleteFolderAsync(Folder folder, CancellationToken ct = default)
    {
        _db.Folders.Remove(folder);
        await _db.SaveChangesAsync(ct);
    }


}
