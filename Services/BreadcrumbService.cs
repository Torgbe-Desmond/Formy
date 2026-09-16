using Formify.Api.Data;
using Formify.Api.Dtos;
using Formify.Api.Exceptions;
using interfaces;
using Microsoft.EntityFrameworkCore;

namespace Formify.Api.Services;



/// <summary>
/// Recursively builds a breadcrumb trail from any entity back to the root
/// (project → folder → file). Direct port of Node's breadcrumb.service.js.
/// </summary>
public class BreadcrumbService : IBreadcrumbService
{
    private static readonly Dictionary<string, int> Levels = new()
    {
        ["project"] = 0,
        ["folder"] = 1,
        ["file"] = 2,
    };

    private readonly AppDbContext _db;

    public BreadcrumbService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<BreadcrumbNode>> GetBreadcrumbAsync(string type, Guid id, CancellationToken ct = default)
    {
        var node = await FetchNodeAsync(type, id, ct);

        var parentType = ParentType(type);
        if (parentType is null)
        {
            return new List<BreadcrumbNode> { node };
        }

        var ancestors = await GetBreadcrumbAsync(parentType, node.ParentId!.Value, ct);
        ancestors.Add(node);
        return ancestors;
    }

    private async Task<BreadcrumbNode> FetchNodeAsync(string type, Guid id, CancellationToken ct)
    {
        switch (type)
        {
            case "project":
                {
                    var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
                        ?? throw new NotFoundException($"Project {id} not found");
                    return new BreadcrumbNode(project.Id, project.Name, "project", Levels["project"], null);
                }
            case "folder":
                {
                    var folder = await _db.Folders.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct)
                        ?? throw new NotFoundException($"Folder {id} not found");
                    return new BreadcrumbNode(folder.Id, folder.Name, "folder", Levels["folder"], folder.ProjectId);
                }
            case "file":
                {
                    var file = await _db.AppFiles.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct)
                        ?? throw new NotFoundException($"File {id} not found");
                    return new BreadcrumbNode(file.Id, file.Name, "file", Levels["file"], file.FolderId);
                }
            default:
                throw new AppException($"Unknown entity type: {type}", 400);
        }
    }

    private static string? ParentType(string type) => type switch
    {
        "file" => "folder",
        "folder" => "project",
        _ => null,
    };
}
