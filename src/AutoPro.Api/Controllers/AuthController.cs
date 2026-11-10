using AutoPro.Application.Contracts.Auth;
using AutoPro.Infrastructure.Persistence;
using AutoPro.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AutoProDbContext db, JwtTokenGenerator tokens) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await db.StaffUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == request.UserName && u.IsActive, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        var (token, expires) = tokens.CreateToken(user);
        return Ok(new LoginResponse(token, expires, user.Id, user.DisplayName, user.Role));
    }
}
