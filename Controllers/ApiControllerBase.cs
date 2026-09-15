using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Formify.Api.Controllers;

/// <summary>
/// All protected controllers derive from this. CurrentUserId is the equivalent
/// of Node's req.userId, set by middleware/auth.js after verifying the JWT —
/// here it comes from ASP.NET Core's own JwtBearer authentication instead.
/// </summary>
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? throw new UnauthorizedAccessException("Missing user id claim"));
}
