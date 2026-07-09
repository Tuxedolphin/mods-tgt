using Backend.DTOs;

namespace Backend.Services.Auth;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);

    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);

    Task ForgotPasswordAsync(ForgotPasswordRequest request);

    Task ResetPasswordAsync(ResetPasswordRequest request);

    Task UpdatePasswordAsync(UpdatePasswordRequest request, string accessToken);
    Task LogoutAsync(string accessToken);
    Task LogoutAllAccountsAsync(string accessToken);
}
