using AutoPro.Domain.Enums;

namespace AutoPro.Application.Contracts.Auth;

public record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string DisplayName,
    UserRole Role);
