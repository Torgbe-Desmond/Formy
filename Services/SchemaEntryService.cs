using Formify.Api.Data;
using Formify.Api.Exceptions;
using Formify.Api.Models;
using interfaces;
using Microsoft.EntityFrameworkCore;

namespace Formify.Api.Services;

public class SchemaEntryService : ISchemaEntryService
{
    private readonly AppDbContext _db;

    public SchemaEntryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task DeleteSchemaEntry(Guid schemaTemplateId, string SchemaEntryName, CancellationToken ct = default)
    {
        SchemaEntry? schemaEntry = await _db.SchemaEntries.FirstOrDefaultAsync(s => s.SchemaTemplateId == schemaTemplateId && s.Name == SchemaEntryName);
        if (schemaEntry == null) throw new NotFoundException("Schema not found.");
        _db.SchemaEntries.Remove(schemaEntry);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> UpdateEntrySchema(
        Guid schemaTemplateId,
        string schemaEntryName,
        CancellationToken ct = default)
    {
        try
        {
            SchemaTemplate? schemaTemplate = await _db.SchemaTemplates
            .FirstOrDefaultAsync(s => s.Id == schemaTemplateId, ct);

            if (schemaTemplate == null) throw new NotFoundException("Schema Template not found");

            schemaTemplate.EntrySchema = schemaEntryName;
            schemaTemplate.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

}