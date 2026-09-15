using Formify.Api.Controllers;
using Formify.Api.Dtos;
using interfaces;
using Microsoft.AspNetCore.Mvc;

[Route("api")]
public class SchemaEntryController : ApiControllerBase
{
    private readonly ISchemaEntryService _schemaEntryService;
    public SchemaEntryController(ISchemaEntryService schemaEntryService)
    {
        this._schemaEntryService = schemaEntryService;
    }

    [HttpDelete("schema-template/{schemaTemplateId:guid}")]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid schemaTemplateId, [FromBody] DeleteSchemaRequest request, CancellationToken ct = default)
    {
        try
        {
            await _schemaEntryService.DeleteSchemaEntry(schemaTemplateId, request.SchemaEntryName, ct);
            return NoContent();
        }
        catch (Exception)
        {
            throw;
        }

    }
}