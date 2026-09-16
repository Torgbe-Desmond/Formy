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

}