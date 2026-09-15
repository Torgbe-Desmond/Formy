using System.IO.Compression;
using System.Text.RegularExpressions;
using Formify.Api.Dtos;
using Formify.Api.Exceptions;
using Formify.Api.Models;
using interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Formify.Api.Controllers;

[Route("api/export")]
public class ExportController : ApiControllerBase
{
    private readonly IPdfExportService _pdf;
    private readonly IFolderSerivce _folderSerivce;
    private readonly IFileService _fileService;
    private readonly ISchemaService _schemaService;
    private readonly IFileContentService _fileContentService;

    public ExportController(
        IPdfExportService pdf,
        IFolderSerivce folderSerivce,
        IFileService fileService,
        ISchemaService schemaService,
        IFileContentService fileContentService
        )
    {
        _pdf = pdf;
        _folderSerivce = folderSerivce;
        _fileService = fileService;
        _schemaService = schemaService;
        _fileContentService = fileContentService;

    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] PdfPreviewRequest req, CancellationToken ct)
    {
        var pdfBytes = await _pdf.RenderAsync(req.Html, req.Margins, ct);
        return File(pdfBytes, "application/pdf");
    }

    [HttpPost("pdf/zip/{folderId:guid}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPdfZip(
        Guid folderId,
        CancellationToken ct)
    {
        try
        {
            Folder? folder = await _folderSerivce.GetFolderById(folderId, ct);

            if (folder == null) throw new NotFoundException($"Foldef with id ${folderId} not found");

            List<AppFile> appFiles = await _fileService.GetFileListByFolderId(folderId, ct);

            SchemaTemplate? schemaTemplate = await _schemaService.GetSchemaAsync(folderId, CurrentUserId, ct);

            if (schemaTemplate == null || schemaTemplate.Schemas == null)
            {
                throw new BadRequestException($"Schema has not been defined for folder with name {folder.Name}");
            }

            SchemaEntry? schemaEntry = schemaTemplate.Schemas.Find(schema =>
                schema.Name == schemaTemplate.EntrySchema && schemaTemplate.EntrySchema != "Main");

            if (schemaEntry == null)
            {
                throw new BadRequestException($"Schema has not been defined for folder with name {folder.Name}");
            }

            string zipPath = await _pdf.RenderFolderAsZipAsync(appFiles, schemaEntry, folder.Name, ct);

            var stream = new FileStream(
            zipPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            options: FileOptions.Asynchronous | FileOptions.DeleteOnClose);

            return File(stream, "application/zip", $"{folder.Name}.zip");
        }
        catch (Exception)
        {
            throw;
        }
    }


    [HttpPost("pdf/{id}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPdf(string id, ExportPdfRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Html))
                throw new BadRequestException("html is required");

            int margins = request.Margins is >= 0 and <= 200 ? request.Margins.Value : 60;
            byte[] pdfBytes = await _pdf.RenderAsync(request.Html, margins, ct);

            string safeName = string.IsNullOrWhiteSpace(request.Filename)
                ? "document"
                : Regex.Replace(request.Filename.Trim(), "[/\\\\?%*:|\"<>]", "_");
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "document";

            return File(pdfBytes, "application/pdf", $"{safeName}.pdf");
        }
        catch (Exception)
        {
            throw;
        }
    }
}
