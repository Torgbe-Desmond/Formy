using System.ComponentModel.DataAnnotations;

namespace Formify.Api.Models;

/// <summary>
/// Shared Id/CreatedAt/UpdatedAt fields — the SQL equivalent of Mongoose's
/// { timestamps: true } option used on every model in the Node app.
/// </summary>
public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class User : AuditableEntity
{
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public List<Project> Projects { get; set; } = new();
}

public class Project : AuditableEntity
{
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    public List<Folder> Folders { get; set; } = new();
    public int FolderCount() => Folders.Count;
}

public class Folder : AuditableEntity
{
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public SchemaTemplate? Schema { get; set; }
    public List<AppFile> Files { get; set; } = new();
    public bool HasSchema() => Schema?.EntrySchema != "Main";
    public int FileCount() => Files.Count;
}

/// <summary>
/// A folder's schema template. Holds one or more named schema definitions
/// (<see cref="Schemas"/>); <see cref="EntrySchema"/> names the default one
/// used to render files in this folder. Mirrors the Node app's multi-schema
/// SchemaTemplate model (schemas map + entrySchema).
/// </summary>
public class SchemaTemplate : AuditableEntity
{
    public Guid FolderId { get; set; }
    public Folder? Folder { get; set; }

    [MaxLength(200)]
    public string EntrySchema { get; set; } = "Main";
    public List<SchemaEntry> Schemas { get; set; } = new();
}

/// <summary>One named schema (schemaYaml/templateHtml/templateCss) within a SchemaTemplate.</summary>
public class SchemaEntry : AuditableEntity
{
    public Guid SchemaTemplateId { get; set; }
    public SchemaTemplate? SchemaTemplate { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public string SchemaYaml { get; set; } = string.Empty;
    public string TemplateHtml { get; set; } = string.Empty;
    public string TemplateCss { get; set; } = string.Empty;
}

public class AppFile : AuditableEntity
{
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    public Guid FolderId { get; set; }
    public Folder? Folder { get; set; }

    /// <summary>
    /// Points at the row in FileContents holding the actual HTML/content.
    /// Replaces the Node app's storageKey (a Mongo ObjectId string) now that
    /// content always lives in SQL Server rather than an external store.
    /// </summary>
    public Guid ContentId { get; set; }

    public long SizeBytes { get; set; }

    public List<AppFileMetadata> Metadata { get; set; } = new();
}

public class AppFileMetadata : AuditableEntity
{
    public Guid FileId { get; set; }
    public AppFile? File { get; set; }

    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

/// <summary>Raw file content, stored directly in SQL Server (no external/blob storage).</summary>
public class FileContent : AuditableEntity
{
    public string Content { get; set; } = string.Empty;
}


