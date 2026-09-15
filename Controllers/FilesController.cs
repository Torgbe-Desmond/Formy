using System.Text;
using Extensions;
using Formify.Api.Data;
using Formify.Api.Dtos;
using Formify.Api.Exceptions;
using Formify.Api.Models;
using interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Formify.Api.Controllers;

[Route("api")]
public class FilesController : ApiControllerBase
{
    private readonly IFileContentService _fileContentService;
    private readonly IFolderSerivce _folderService;
    private readonly IFileService _fileService;
    private readonly IProjectService _projectService;
    private readonly IMetaDataService _metaDataService;

    public FilesController(
        IFileContentService fileContentService,
        IFolderSerivce folderSerivce,
        IProjectService projectService,
        IFileService fileService,
        IMetaDataService metaDataService
        )
    {
        _fileContentService = fileContentService;
        _folderService = folderSerivce;
        _projectService = projectService;
        _fileService = fileService;
        _metaDataService = metaDataService;
    }

    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ResponseModel<AppFileDto>), StatusCodes.Status200OK)]
    [HttpGet("folders/{folderId:guid}/files")]
    public async Task<IActionResult> GetByFolder(Guid folderId, CancellationToken ct)
    {
        try
        {

            List<AppFile> files = await _fileService.GetFileListByFolderId(folderId, ct);
            ResponseModel<List<AppFileDto>> responseModel = new ResponseModel<List<AppFileDto>>
            {
                Data = files.Select(file => file.ToDto()).ToList(),
                Message = "Files fetched succesfully",
            };

            return Ok(responseModel);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpGet("files/{id:guid}")]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseModel<AppFileDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            AppFile file = await _fileService.GetFileById(id, ct)
             ?? throw new NotFoundException("File not found");

            Folder folder = await _folderService.GetFolderById(file.FolderId, ct)
                ?? throw new NotFoundException("Folder not found");

            Project? project = await _projectService.GetProjectAsync(folder.ProjectId, CurrentUserId, ct);
            if (project is null || project.OwnerId != CurrentUserId) throw new ForbiddenException();

            string content = await _fileContentService.DownloadFileContentAsync(file.ContentId, ct);

            AppFileDetailDto appFileDetailDto = new AppFileDetailDto(file.ToDto(), content);

            ResponseModel<AppFileDetailDto> responseModel = new ResponseModel<AppFileDetailDto>
            {
                Data = appFileDetailDto,
                Message = "File fetched successfully",
                StatusCode = StatusCodes.Status200OK
            };

            return Ok(responseModel);
        }

        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost("folders/{folderId:guid}/files")]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ResponseModel<AppFileDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(Guid folderId, CreateFileRequest request, CancellationToken ct)
    {
        try
        {

            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 500)
                throw new BadRequestException("Name is required");

            string content = request.Content ?? string.Empty;

            AppFileDto file = await _fileService.AddAsync(folderId, request, ct);

            AppFileDetailDto appFileDetailDto = new AppFileDetailDto(file, content);

            ResponseModel<AppFileDetailDto> responseModel = new ResponseModel<AppFileDetailDto>
            {
                Data = appFileDetailDto,
                Message = "Files retrived succesfully",
                StatusCode = StatusCodes.Status201Created
            };

            return StatusCode(StatusCodes.Status201Created, responseModel);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPut("files/{fileId:guid}")]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ResponseModel<AppFileDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid fileId, UpdateFileRequest request, CancellationToken ct)
    {
        try
        {

            AppFile file = await _fileService.GetFileById(fileId, ct)
                ?? throw new NotFoundException("File not found");

            if (request.Name is not null) file.Name = request.Name;

            if (request.Content is not null)
            {
                await _fileContentService.UpdateFileContentAsync(file.ContentId, request.Content, ct);
                file.SizeBytes = Encoding.UTF8.GetByteCount(request.Content);
            }

            if (request.Metadata is not null)
            {
                await _metaDataService.UpdateMetadataAsync(file.Id, request.Metadata);
            }

            var savedContent = request.Content ?? await _fileContentService.DownloadFileContentAsync(file.ContentId, ct);

            AppFile updatedAppFile = await _fileService.UpdateFileAsync(file);

            ResponseModel<AppFileUpdateResultDto> responseModel = new ResponseModel<AppFileUpdateResultDto>
            {
                Data = new AppFileUpdateResultDto(updatedAppFile.ToDto(), savedContent),
                Message = "File updated successfully",
                StatusCode = StatusCodes.Status200OK,
            };

            return Ok(responseModel);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPut("files/{fileId:guid}/rename")]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateFileName(Guid fileId, [FromBody] RenameFileRequest request, CancellationToken ct)
    {
        try
        {
            await _fileService.RenameFileAsync(fileId, request.Name, ct);
            return NoContent();
        }
        catch (Exception)
        {
            throw;
        }
    }


    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(IActionResult), StatusCodes.Status204NoContent)]
    [HttpDelete("files/{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        try
        {

            var file = await _fileService.GetFileById(id, ct)
                ?? throw new NotFoundException("File not found");

            await _fileService.DeleteFileAsync(file.Id, ct);

            return NoContent();
        }
        catch (Exception)
        {
            throw;
        }
    }
}
