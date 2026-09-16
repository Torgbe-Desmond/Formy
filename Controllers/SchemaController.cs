using Extensions;
using Formify.Api.Dtos;
using Formify.Api.Models;
using interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Formify.Api.Controllers;

[Route("api/folders/{folderId:guid}/schema")]
public class SchemaController : ApiControllerBase
{
    private readonly ISchemaService _schemas;
    public SchemaController(ISchemaService schemas)
    {
        _schemas = schemas;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SchemaTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(Guid folderId, CancellationToken ct)
    {
        SchemaTemplate? schema = await _schemas.GetSchemaAsync(folderId, CurrentUserId, ct);
        return Ok(schema.ToDto());
    }

    [HttpGet("schemas/{projectId:guid}")]
    [ProducesResponseType(typeof(List<SchemaEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectSchemas(
        Guid projectId,
        CancellationToken ct)
    {
        List<SchemaEntry> schemaEntries = await _schemas.GetSchemasAsync(projectId, ct);
        List<SchemaEntryDto> result = schemaEntries.Select(s => s.ToDto()).ToList();
        return Ok(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(SchemaTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upsert(Guid folderId, UpsertSchemaRequest request, CancellationToken ct)
    {
        string entrySchema;
        List<InsertionSchema> entries = new List<InsertionSchema>();

        if (request.Schemas is { Count: > 0 })
        {
            entrySchema = string.IsNullOrWhiteSpace(request.EntrySchema) ? "Main" : request.EntrySchema!;
            foreach (KeyValuePair<string, SchemaEntryRequest> entry in request.Schemas)
            {

                InsertionSchema insertionSchema = new InsertionSchema(
                    entry.Key,
                    entry.Value.SchemaYaml ?? "",
                    entry.Value.TemplateHtml ?? "",
                    entry.Value.TemplateCss ?? "");

                entries.Add(insertionSchema);
            }
        }
        else
        {
            entrySchema = "Main";

            InsertionSchema insertionSchema = new InsertionSchema(
                entrySchema,
                request.SchemaYaml ?? "",
                request.TemplateHtml ?? "",
                request.TemplateCss ?? "");

            entries.Add(insertionSchema);
        }

        var schema = await _schemas.UpsertSchemaAsync(folderId, CurrentUserId, entrySchema, entries, ct);
        return Ok(schema.ToDto());
    }


}