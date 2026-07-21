using System.Net.Http.Headers;
using Backend.DTOs;
using Backend.Exceptions;
using Supabase;

namespace Backend.Services.Auth;

public class AuthService(
    Client supabase,
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment web
) : IAuthService
{
    private readonly Client _supabase = supabase;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private readonly IWebHostEnvironment _webHostEnvironment = web;

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var response = await _supabase.Auth.SignInWithPassword(request.Email, request.Password);
        return ExtractTokens(response);
    }

    public async Task LogoutAsync(string accessToken)
    {
        var response = await PostGotrueAsync("logout?scope=local", null, accessToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new ExternalServiceException(
                $"Logout failed. Status: {(int)response.StatusCode} {response.StatusCode}. Details: {errorContent}"
            );
        }
    }

    public async Task LogoutAllAccountsAsync(string accessToken)
    {
        var response = await PostGotrueAsync("logout?scope=global", null, accessToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new ExternalServiceException(
                $"Logout from all devices failed. Status: {(int)response.StatusCode} {response.StatusCode}. Details: {errorContent}"
            );
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // We make a bit of cheat here, supabase client prefers if we just refresh directly from the frontend client,
        // so we mimic how we'll do it from the frontend

        var response = await PostGotrueAsync(
            "token?grant_type=refresh_token",
            new { refresh_token = request.RefreshToken }
        );

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new ExternalServiceException(
                $"Failed to refresh token. Status: {(int)response.StatusCode} {response.StatusCode}. Details: {errorContent}"
            );
        }

        var supabaseResponse = await response.Content.ReadFromJsonAsync<SupabaseTokenResponse>();
        return ValidateTokens(
            supabaseResponse == null
                ? null
                : new AuthResponse
                {
                    AccessToken = supabaseResponse.AccessToken,
                    RefreshToken = supabaseResponse.RefreshToken,
                    ExpiresIn = supabaseResponse.ExpiresIn,
                    TokenType = supabaseResponse.TokenType,
                }
        );
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        var session = await _supabase.Auth.SignUp(request.Email, request.Password);

        if (session?.AccessToken == null)
        {
            return new RegisterResponse
            {
                RequiresEmailConfirmation = true,
                Message =
                    "Registration successful. Please check your email to confirm your account.",
            };
        }

        return new RegisterResponse
        {
            RequiresEmailConfirmation = false,
            Message = "Registration successful.",
            Session = ExtractTokens(session),
        };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        string redirectUrl;
        if (_webHostEnvironment.IsDevelopment())
        {
            redirectUrl = "http://localhost:5173";
        }
        else
        {
            redirectUrl = "https://mods-tgt.com";
        }

        var response = await PostGotrueAsync(
            $"recover?redirect_to={Uri.EscapeDataString(redirectUrl)}",
            new { email = request.Email }
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine(
                $"Recovery email failed, with status {(int)response.StatusCode} {response.StatusCode}. Details: {error} "
            );

            throw new ExternalServiceException(error);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var verifyResponse = await PostGotrueAsync(
            "verify",
            new { type = "recovery", token_hash = request.TokenHash }
        );

        if (!verifyResponse.IsSuccessStatusCode)
        {
            string error = await verifyResponse.Content.ReadAsStringAsync();
            Console.WriteLine(
                $"Recovery token invalid or expired. Status: {(int)verifyResponse.StatusCode}. Details: {error}"
            );
            throw new ExternalServiceException(error);
        }

        SupabaseTokenResponse session =
            await verifyResponse.Content.ReadFromJsonAsync<SupabaseTokenResponse>()
            ?? throw new ExternalServiceException("Verify returned no session");

        var updateResponse = await PutGotrueAsync(
            "user",
            new { password = request.Password },
            session.AccessToken
        );

        if (!updateResponse.IsSuccessStatusCode)
        {
            string error = await updateResponse.Content.ReadAsStringAsync();
            Console.WriteLine(
                $"Password update failed. Status: {(int)updateResponse.StatusCode}. Details: {error}"
            );
            throw new ExternalServiceException(error);
        }

        var logoutResponse = await PostGotrueAsync(
            "logout?scope=global",
            null,
            session.AccessToken
        );

        if (!logoutResponse.IsSuccessStatusCode)
        {
            string error = await logoutResponse.Content.ReadAsStringAsync();

            // TODO: Add proper logging for this
            Console.WriteLine($"Logout error: {error}");

            throw new ExternalServiceException(error);
        }
    }

    public async Task UpdatePasswordAsync(
        UpdatePasswordRequest request,
        string accessToken,
        Guid userId
    )
    {
        var response = await PutGotrueAsync(
            "user",
            new { password = request.NewPassword, current_password = request.OldPassword },
            accessToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new ExternalServiceException(
                $"Failed to update password. Status: {(int)response.StatusCode} {response.StatusCode} Error: {error} "
            );
        }
    }

    private HttpClient CreateGotrueClient(string? accessToken)
    {
        var http = _httpClientFactory.CreateClient("Gotrue");

        if (accessToken != null)
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken
            );
        }

        return http;
    }

    private Task<HttpResponseMessage> PostGotrueAsync(
        string path,
        object? body,
        string? accessToken = null
    )
    {
        var http = CreateGotrueClient(accessToken);
        return body == null ? http.PostAsync(path, null) : http.PostAsJsonAsync(path, body);
    }

    private Task<HttpResponseMessage> PutGotrueAsync(
        string path,
        object body,
        string? accessToken = null
    )
    {
        var http = CreateGotrueClient(accessToken);
        return http.PutAsJsonAsync(path, body);
    }

    private static AuthResponse ExtractTokens(Supabase.Gotrue.Session? response)
    {
        if (response == null)
            throw new ExternalServiceException("Supabase returned no session.");

        return ValidateTokens(
            new AuthResponse
            {
                AccessToken = response.AccessToken!,
                RefreshToken = response.RefreshToken!,
                ExpiresIn = response.ExpiresIn,
                TokenType = response.TokenType!,
            }
        );
    }

    private static AuthResponse ValidateTokens(AuthResponse? response)
    {
        if (response == null)
            throw new ExternalServiceException("Supabase returned no authentication response.");
        if (string.IsNullOrEmpty(response.AccessToken))
            throw new ExternalServiceException("Supabase returned no access token.");
        if (string.IsNullOrEmpty(response.RefreshToken))
            throw new ExternalServiceException("Supabase returned no refresh token.");
        if (string.IsNullOrEmpty(response.TokenType))
            throw new ExternalServiceException("Supabase returned no token type.");
        if (response.ExpiresIn == 0)
            throw new ExternalServiceException("Supabase returned no expiration time.");

        return response;
    }
}
