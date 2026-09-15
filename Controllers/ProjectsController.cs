using Extensions;
using Formify.Api.Dtos;
using Formify.Api.Exceptions;
using Formify.Api.Models;
using interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Formify.Api.Controllers;

[Route("api/projects")]
public class ProjectsController : ApiControllerBase
{
    private readonly IProjectService _projects;

    public ProjectsController(IProjectService projects)
    {
        _projects = projects;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        try
        {
            List<Project> projects = await _projects.GetAllProjectsAsync(CurrentUserId, ct);
            return Ok(projects.Select(project => project.ToDto()));
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            Project project = await _projects.GetProjectAsync(id, CurrentUserId, ct);
            return Ok(project.ToDto());
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(ProjectRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
                throw new BadRequestException("Name is required and must be 200 characters or less");

            var project = await _projects.CreateProjectAsync(request.Name, CurrentUserId, ct);
            return StatusCode(StatusCodes.Status201Created, project.ToDto());
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Rename(Guid id, ProjectRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
                throw new BadRequestException("Name is required and must be 200 characters or less");

            Project project = await _projects.RenameProjectAsync(id, CurrentUserId, request.Name, ct);
            return Ok(project.ToDto());
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        try
        {
            await _projects.DeleteProjectAsync(id, CurrentUserId, ct);
            return NoContent();
        }
        catch (Exception)
        {
            throw;
        }
    }
}