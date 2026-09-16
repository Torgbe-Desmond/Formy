using Formify.Api.Data;
using Formify.Api.Dtos;
using Formify.Api.Exceptions;
using Formify.Api.Models;
using interfaces;
using Microsoft.EntityFrameworkCore;
namespace Formify.Api.Services;

public class SchemaService : ISchemaService
{
    private readonly AppDbContext _db;

    public SchemaService(AppDbContext db)
    {
        _db = db;
    }

    private async Task AssertFolderOwnershipAsync(Guid folderId, Guid userId, CancellationToken ct)
    {
        Folder folder = await _db.Folders.FirstOrDefaultAsync(f => f.Id == folderId, ct)
    ?? throw new NotFoundException("Folder not found");
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == folder.ProjectId, ct);
        if (project is null || project.OwnerId != userId) throw new ForbiddenException();

    }

    public async Task<SchemaTemplate> GetSchemaAsync(Guid folderId, Guid userId, CancellationToken ct = default)
    {
        return await _db.SchemaTemplates
            .Include(s => s.Schemas)
            .FirstOrDefaultAsync(s => s.FolderId == folderId, ct)
            ?? throw new NotFoundException("Schema not found");
    }

    public async Task<List<SchemaEntry>> GetSchemasAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _db.Projects
            .Where(p => p.Id == projectId)
            .Include(p => p.Folders)
                .ThenInclude(f => f.Schema)
                    .ThenInclude(s => s.Schemas)
            .FirstOrDefaultAsync(ct);

        if (project == null)
        {
            throw new NotFoundException($"Project with id {projectId} not found");
        }

        var schemaEntries = new List<SchemaEntry>();

        foreach (Folder folder in project.Folders)
        {
            SchemaTemplate? currentSchema = folder?.Schema;
            if (currentSchema == null) continue;

            string? entrySchema = currentSchema.EntrySchema;
            SchemaEntry? schemaEntry = currentSchema.Schemas
                ?.FirstOrDefault(schema => schema.Name == entrySchema && entrySchema != "Main");

            if (schemaEntry == null) continue;

            schemaEntries.Add(schemaEntry);
        }

        return schemaEntries;
    }

    public async Task<SchemaTemplate> AddEntrySchemaAsync(
        Guid folderId,
        CancellationToken ct = default
    )
    {
        SchemaTemplate? schemaTemplate = await _db.SchemaTemplates
               .FirstOrDefaultAsync(s => s.FolderId == folderId, ct);

        if (schemaTemplate != null)
        {
            throw new BadRequestException($"Template for the folder with id {folderId} already exist");
        }

        schemaTemplate = new SchemaTemplate { FolderId = folderId };
        _db.SchemaTemplates.Add(schemaTemplate);

        SchemaEntry schemaEntry = new SchemaEntry
        {
            SchemaTemplateId = schemaTemplate.Id,
            Name = "Main"
        };

        _db.SchemaEntries.Add(schemaEntry);

        await _db.SaveChangesAsync();
        return schemaTemplate;

    }

    public async Task<SchemaTemplate> UpdateSchemaAsync(
        Guid folderId,
        string entrySchema,
        IEnumerable<InsertionSchema> entries,
        CancellationToken ct = default)
    {
        SchemaTemplate? schemaTemplate = await _db.SchemaTemplates
      .FirstOrDefaultAsync(s => s.FolderId == folderId, ct);

        if (schemaTemplate == null) throw new NotFoundException("Schema not found");

        schemaTemplate.EntrySchema = entrySchema;
        schemaTemplate.UpdatedAt = DateTime.UtcNow;

        List<SchemaEntry> schemaEntry = await _db.SchemaEntries
        .Where(schemaEntry => schemaEntry.SchemaTemplateId == schemaTemplate.Id)
        .ToListAsync();

        var incoming = entries.ToDictionary(e => e.Name, e => e);
        var existingSchemaEntryByName = schemaEntry.ToDictionary(s => s.Name, s => s);

        // Checking for schema names not available in the incoming request
        foreach (var stale in existingSchemaEntryByName.Values.Where(s => !incoming.ContainsKey(s.Name)))
        {
            schemaEntry.Remove(stale);
            _db.SchemaEntries.Remove(stale);
        }

        foreach (var e in incoming.Values)
        {
            /*
             Update the existing schema entry information with the incoming one
             if the name is still available in the existingSchemaEntryByName dictionary else add it as 
             a new entry
            */
            if (existingSchemaEntryByName.TryGetValue(e.Name, out var existingschemaEntry))
            {

                SchemaEntry? existSchemaEntry = await _db.SchemaEntries
                .FirstOrDefaultAsync(schemaEntry => schemaEntry.SchemaTemplateId == schemaTemplate.Id && schemaEntry.Name == e.Name);

                if (existSchemaEntry != null)
                {
                    existSchemaEntry.SchemaYaml = e.SchemaYaml;
                    existSchemaEntry.TemplateHtml = e.TemplateHtml;
                    existSchemaEntry.TemplateCss = e.TemplateCss;
                    existSchemaEntry.UpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
            }
            else
            {
                SchemaEntry? existSchemaEntry = await _db.SchemaEntries
                .FirstOrDefaultAsync(schemaEntry => schemaEntry.SchemaTemplateId == schemaTemplate.Id && schemaEntry.Name == e.Name);

                Console.WriteLine($"New:{e.Name}");
                SchemaEntry schemaEntry1 = new SchemaEntry
                {
                    SchemaTemplateId = schemaTemplate.Id,
                    Name = e.Name,
                    SchemaYaml = e.SchemaYaml,
                    TemplateHtml = e.TemplateHtml,
                    TemplateCss = e.TemplateCss,
                };

                _db.SchemaEntries.Add(schemaEntry1);

            }

        }

        await _db.SaveChangesAsync();
        return schemaTemplate;
    }

    public async Task<SchemaTemplate> UpsertSchemaAsync(
        Guid folderId,
        Guid userId,
        string entrySchema,
        IEnumerable<InsertionSchema> entries,
        CancellationToken ct = default)
    {
        await AssertFolderOwnershipAsync(folderId, userId, ct);

        SchemaTemplate? schemaTemplate = await _db.SchemaTemplates
            .FirstOrDefaultAsync(s => s.FolderId == folderId, ct);

        schemaTemplate = await this.UpdateSchemaAsync(folderId, entrySchema, entries, ct);

        try
        {
            await _db.SaveChangesAsync(ct);
            return schemaTemplate;
        }
        catch (DbUpdateConcurrencyException)
        {
            foreach (var entry in _db.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;

            throw new ConflictException(
                "This schema was changed by another request at the same time. Please retry.");
        }

    }
}