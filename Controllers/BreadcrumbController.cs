using Formify.Api.Dtos;
using Formify.Api.Exceptions;
using interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Formify.Api.Controllers;

[Route("api/breadcrumb")]
public class BreadcrumbController : ApiControllerBase
{
    private static readonly HashSet<string> ValidTypes = new() { "project", "folder", "file" };
    private readonly IBreadcrumbService _breadcrumb;

    public BreadcrumbController(IBreadcrumbService breadcrumb)
    {
        _breadcrumb = breadcrumb;
    }

    [HttpGet("{type}/{id:guid}")]
    [ProducesResponseType(typeof(ResponseModel<List<BreadcrumbNode>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string type, Guid id, CancellationToken ct)
    {
        if (!ValidTypes.Contains(type))
        {
            throw new BadRequestException("type must be one of: project, folder, file");
        }

        List<BreadcrumbNode> crumbs = await _breadcrumb.GetBreadcrumbAsync(type, id, ct);

        ResponseModel<List<BreadcrumbNode>> responseModel = new ResponseModel<List<BreadcrumbNode>>
        {
            Data = crumbs,
            Message = "Login was successful",
            StatusCode = StatusCodes.Status200OK
        };

        return Ok(responseModel);
    }
}
