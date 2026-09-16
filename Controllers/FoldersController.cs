using Extensions;
using Formify.Api.Data;
using Formify.Api.Dtos;
using Formify.Api.Exceptions;
using Formify.Api.Models;
using interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Formify.Api.Controllers;

[Route("api")]
public class FoldersController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFileContentService _content;
    private readonly IFileService _fileService;
    private readonly IFolderSerivce _folderSerivce;
    private readonly IProjectService _projectService;
    public FoldersController(
        AppDbContext db,
        IFileContentService content,
        IFolderSerivce folderSerivce,
        IFileService fileService,
        IProjectService projectSerivce
        )
    {
        _db = db;
        _content = content;
        _folderSerivce = folderSerivce;
        _projectService = projectSerivce;
        _fileService = fileService;
    }

    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ResponseModel<AppFileDetailDto>), StatusCodes.Status200OK)]
    [HttpGet("projects/{projectId:guid}/folders")]
    public async Task<IActionResult> GetByProject(Guid projectId, CancellationToken ct)
    {
        Project project = await _projectService.GetProjectAsync(projectId, CurrentUserId, ct)
                      ?? throw new NotFoundException("Project not found");

        if (project.OwnerId != CurrentUserId) throw new ForbiddenException();

        List<Folder> folders = await _folderSerivce.GetFolderListByProjectId(projectId, ct);

        ResponseModel<List<FolderDto>> responseModel = new ResponseModel<List<FolderDto>>
        {
            Data = folders.Select(folder => folder.ToDto()).ToList(),
            Message = "Folders fetched succesfully",
            StatusCode = StatusCodes.Status200OK
        };

        return Ok(responseModel);
    }


    [HttpPost("projects/{projectId:guid}/folders")]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseModel<AppFileDetailDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid projectId, FolderRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
            throw new BadRequestException("Name is required");

        Project project = await _projectService.GetProjectAsync(projectId, CurrentUserId, ct)
            ?? throw new NotFoundException("Project not found");

        if (project.OwnerId != CurrentUserId) throw new ForbiddenException();

        Folder folder = await _folderSerivce.AddFolderAsync(projectId, request, ct);

        ResponseModel<FolderDto> responseModel = new ResponseModel<FolderDto>
        {
            Data = folder.ToDto(),
            Message = "Folder fetched succesfully",
            StatusCode = StatusCodes.Status200OK
        };

        return StatusCode(StatusCodes.Status201Created, responseModel);
    }

    [HttpPut("folders/{folderId:guid}")]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseModel<AppFileDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Rename(Guid folderId, FolderRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
            throw new BadRequestException("Name is required");

        Folder folder = await _folderSerivce.UpdateFolderAsync(folderId, request, ct);

        ResponseModel<FolderDto> responseModel = new ResponseModel<FolderDto>
        {
            Data = folder.ToDto(),
            Message = "Folder fetched succesfully",
            StatusCode = StatusCodes.Status200OK
        };

        return StatusCode(StatusCodes.Status200OK, responseModel);
    }

    [HttpDelete("folders/{folderId:guid}")]
    public async Task<IActionResult> Remove(Guid folderId, CancellationToken ct)
    {
        Folder folder = await _folderSerivce.GetFolderById(folderId, ct)
                ?? throw new NotFoundException("Folder not found");

        Project project = await _projectService.GetProjectAsync(folder.ProjectId, CurrentUserId, ct)
            ?? throw new NotFoundException("Project not found");
        if (project.OwnerId != CurrentUserId) throw new ForbiddenException();

        List<AppFile> appFiles = await _fileService.GetFileListByFolderId(folder.Id);

        IEnumerable<Guid> contentIds = appFiles.Select(f => f.ContentId);

        foreach (var contentId in contentIds)
        {
            try { await _content.DeleteFileContentAsync(contentId, ct); }
            catch { }
        }

        await _folderSerivce.DeleteFolderAsync(folder);
        return NoContent();
    }
}
