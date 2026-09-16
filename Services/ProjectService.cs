using Formify.Api.Data;
using Formify.Api.Exceptions;
using Formify.Api.Models;
using interfaces;
using Microsoft.EntityFrameworkCore;

namespace Formify.Api.Services;

public class ProjectService : IProjectService
{
    private readonly AppDbContext _db;
    private readonly IFileContentService _content;

    public ProjectService(AppDbContext db, IFileContentService content)
    {
        _db = db;
        _content = content;
    }

    public async Task<List<Project>> GetAllProjectsAsync(Guid ownerId, CancellationToken ct = default)
    {
        return await _db.Projects
                    .Where(p => p.OwnerId == ownerId)
                    .Include(p => p.Folders)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(ct);
    }

    public async Task<Project> GetProjectAsync(Guid id, Guid ownerId, CancellationToken ct = default)
    {
        Project project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Project not found");
        if (project.OwnerId != ownerId) throw new ForbiddenException();
        return project;
    }

    public async Task<Project> CreateProjectAsync(string name, Guid ownerId, CancellationToken ct = default)
    {
        Project project = new Project { Name = name.Trim(), OwnerId = ownerId };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);
        return project;
    }

    public async Task<Project> RenameProjectAsync(Guid id, Guid ownerId, string name, CancellationToken ct = default)
    {
        Project project = await GetProjectAsync(id, ownerId, ct);
        project.Name = name.Trim();
        project.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return project;
    }

    public async Task DeleteProjectAsync(Guid id, Guid ownerId, CancellationToken ct = default)
    {

        var project = await GetProjectAsync(id, ownerId, ct);

        // Cascade: delete file content (not a real FK, so this must be manual),
        // then remove the project — SQL Server cascade deletes handle
        // Folders → AppFiles/SchemaTemplates/Metadata below it.
        var folderIds = await _db.Folders.Where(f => f.ProjectId == project.Id).Select(f => f.Id).ToListAsync(ct);
        var contentIds = await _db.AppFiles.Where(f => folderIds.Contains(f.FolderId)).Select(f => f.ContentId).ToListAsync(ct);

        foreach (var contentId in contentIds)
        {
            try { await _content.DeleteFileContentAsync(contentId, ct); }
            catch { /* ignore missing storage entries */ }
        }

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(ct);
    }
}