namespace Formify.Api.Dtos;

// ── Auth ─────────────────────────────────────────────────────────────────
public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record UserDto(Guid Id, string Name, string Email);
public record AuthResponse(string Token, UserDto User);

// ── Projects ─────────────────────────────────────────────────────────────
public record ProjectRequest(string Name);
public record ProjectDto(Guid Id, string Name, Guid OwnerId, DateTime CreatedAt, DateTime UpdatedAt, int FolderCount);

// ── Folders ───────────────────────────────────────────────────────────────
public record FolderRequest(string Name);
public record FolderDto(Guid Id, string Name, Guid ProjectId, DateTime CreatedAt, DateTime UpdatedAt, int FileCount, bool HasSchema);

// ── Schema (multi-schema shape) ─────────────────────────────────────────
public record SchemaEntryRequest(string? SchemaYaml, string? TemplateHtml, string? TemplateCss);

public record UpsertSchemaRequest(
    Dictionary<string, SchemaEntryRequest>? Schemas,
    string? EntrySchema,
    string? SchemaYaml,
    string? TemplateHtml,
    string? TemplateCss);

public class PdfPreviewRequest
{
    public string Html { get; set; } = string.Empty;
    public int Margins { get; set; }
}

public record DeleteSchemaRequest(string SchemaEntryName);
public record InsertionSchema(string Name, string SchemaYaml, string TemplateHtml, string TemplateCss);

public record SchemaEntryDto(string SchemaYaml, string TemplateHtml, string TemplateCss);

public record SchemaTemplateDto(
    Guid Id,
    Guid FolderId,
    string EntrySchema,
    Dictionary<string, SchemaEntryDto> Schemas,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ── Files ─────────────────────────────────────────────────────────────────
public record CreateFileRequest(string Name, string? Content, Dictionary<string, string>? Metadata);
public record UpdateFileRequest(string? Name, string? Content, Dictionary<string, string>? Metadata);
public record RenameFileRequest(string Name);
public record ErrorMessage(string message);
public record AppFileMetadataDto(string Key, string Value);

public record AppFileDto(
    Guid Id,
    string Name,
    Guid FolderId,
    long SizeBytes,
    List<AppFileMetadataDto> Metadata,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>GET /files/{id} shape — matches Node's { file, content }.</summary>
public record AppFileDetailDto(AppFileDto File, string Content);

/// <summary>PUT /files/{id} shape — matches Node's { file, savedContent }.</summary>
public record AppFileUpdateResultDto(AppFileDto File, string SavedContent);

// ── Breadcrumb ───────────────────────────────────────────────────────────
public record BreadcrumbNode(Guid Id, string Name, string Type, int Level, Guid? ParentId);

// ── Export ────────────────────────────────────────────────────────────────
public record ExportPdfRequest(string Html, string? Filename, int? Margins);

public class ResponseModel<T>
{
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; } = default;
}

public class ErrorResponseModel()
{
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; } = default;
}