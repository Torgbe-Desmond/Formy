using System.Net.Mime;
using Formify.Api.Data;
using Formify.Api.Dtos;
using Formify.Api.Exceptions;
using Formify.Api.Models;
using interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Formify.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthService _auth;

    public AuthController(AppDbContext db, IAuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ResponseModel<AuthResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length == 0 || request.Name.Length > 200)
            throw new BadRequestException("Name is required");
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new BadRequestException("Valid email is required");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new BadRequestException("Password must be at least 6 characters");

        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists) throw new BadRequestException("Invalid Email or password");

        AuthResponse authResponse = await _auth.RegisterUser(request, ct);

        ResponseModel<AuthResponse> responseModel = new ResponseModel<AuthResponse>
        {
            Data = authResponse,
            Message = "Registration was successful",
            StatusCode = StatusCodes.Status201Created
        };

        return StatusCode(StatusCodes.Status201Created, responseModel);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ResponseModel<AuthResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new BadRequestException("Valid email is required");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new BadRequestException("Password is required");

        string email = request.Email.Trim().ToLowerInvariant();
        User? user = await _auth.FindByEmail(email, ct);
        if (user is null)
            throw new BadRequestException("Invalid email or password");

        if (!_auth.VerifyPassword(request.Password, user.PasswordHash))
            throw new BadRequestException("Invalid email or password");

        var token = _auth.GenerateToken(user);
        AuthResponse authResponse = new AuthResponse(token, new UserDto(user.Id, user.Name, user.Email));

        return Ok(new ResponseModel<AuthResponse>
        {
            Data = authResponse,
            Message = "Login was successful",
            StatusCode = StatusCodes.Status200OK
        });
    }
}
