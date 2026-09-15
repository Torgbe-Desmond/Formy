using Formify.Api.Dtos;
using Formify.Api.Models;
namespace interfaces;

public interface IAuthService
{
    string HashPassword(string plaintext);
    bool VerifyPassword(string plaintext, string hash);
    string GenerateToken(User user);
    Task<AuthResponse> RegisterUser(RegisterRequest user, CancellationToken ct = default);
    Task<User?> FindByEmail(string email, CancellationToken ct = default);
}
public interface IBreadcrumbService
{
    Task<List<BreadcrumbNode>> GetBreadcrumbAsync(string type, Guid id, CancellationToken ct = default);
}

public interface IFileContentService
{
    Task<Guid> UploadFileContentAsync(string content, CancellationToken ct = default);
    Task<string> DownloadFileContentAsync(Guid contentId, CancellationToken ct = default);
    Task UpdateFileContentAsync(Guid contentId, string content, CancellationToken ct = default);
    Task DeleteFileContentAsync(Guid contentId, CancellationToken ct = default);
}

public interface IFileService
{
    Task<AppFile?> GetFileById(Guid fileId, CancellationToken ct = default);
    Task<AppFileDto> AddAsync(Guid folderId, CreateFileRequest request, CancellationToken ct = default);
    Task<List<AppFile>> GetFileListByFolderId(Guid folderId, CancellationToken ct = default);
    Task DeleteFileAsync(Guid fileId, CancellationToken ct = default);
    Task RenameFileAsync(Guid fileId, string newName, CancellationToken ct = default);
    Task<AppFile> UpdateFileAsync(AppFile appFile, CancellationToken ct = default);
}

public interface IFolderSerivce
{
    Task<Folder?> GetFolderById(Guid folderId, CancellationToken ct = default);
    Task<List<Folder>> GetFolderListByProjectId(Guid projectId, CancellationToken ct = default);
    Task<Folder> AddFolderAsync(Guid projectId, FolderRequest request, CancellationToken ct = default);
    Task<Folder> UpdateFolderAsync(Guid folderId, FolderRequest request, CancellationToken ct = default);
    Task DeleteFolderAsync(Folder folder, CancellationToken ct = default);

}

// public interface IProjectSerivce
// {
//     Task<Project?> GetByProjectId(Guid folderId, CancellationToken ct = default);
//     Task DeleteProjectAsync(Project project, CancellationToken ct = default);
// }
public interface IProjectService
{
    Task<List<Project>> GetAllProjectsAsync(Guid ownerId, CancellationToken ct = default);

    Task<Project> GetProjectAsync(Guid id, Guid ownerId, CancellationToken ct = default);

    Task<Project> CreateProjectAsync(string name, Guid ownerId, CancellationToken ct = default);

    Task<Project> RenameProjectAsync(Guid id, Guid ownerId, string name, CancellationToken ct = default);

    Task DeleteProjectAsync(Guid id, Guid ownerId, CancellationToken ct = default);
}

public interface ISchemaService
{
    Task<SchemaTemplate> GetSchemaAsync(Guid folderId, Guid userId, CancellationToken ct = default);
    Task<SchemaTemplate> UpdateSchemaAsync(
            Guid folderId,
            string entrySchema,
            IEnumerable<InsertionSchema> entries,
            CancellationToken ct = default);
    Task<SchemaTemplate> UpsertSchemaAsync(
        Guid folderId,
        Guid userId,
        string entrySchema,
        IEnumerable<InsertionSchema> entries,
        CancellationToken ct = default);
    Task<SchemaTemplate> AddEntrySchemaAsync(
        Guid folderId,
        CancellationToken ct = default
    );
    Task<List<SchemaEntry>> GetSchemasAsync(Guid projectId, CancellationToken ct = default);
}

public interface ISchemaEntryService
{
    Task DeleteSchemaEntry(Guid schemaTemplateId, string SchemaEntryName, CancellationToken ct = default);
}

public interface IPdfExportService
{
    Task<byte[]> RenderAsync(string html, int? marginPx, CancellationToken ct = default);
    Task<string> RenderedContentAsync(string content, AppFile file, SchemaEntry schema);
    Task<string> RenderFolderAsZipAsync(
        List<AppFile> Files,
        SchemaEntry Schema,
        string zipFileNameWithoutExtension = "documents",
        CancellationToken ct = default);
}

public interface IMetaDataService
{
    Task<AppFileMetadata> GetMetadataAsync(Guid fileId, string key, CancellationToken ct = default);
    Task UpdateMetadataAsync(Guid fileId, Dictionary<string, string> metadata);
}
