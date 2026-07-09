using System.Net.Http.Headers;
using Backend.DTOs;
using Backend.Exceptions;
using Backend.Settings;
using Microsoft.Extensions.Options;
using Supabase;

namespace Backend.Services.Auth;

public class AuthService(Client supabase, IOptions<SupabaseSettings> settings, IWebHostEnvironment web) : IAuthService
{
    private readonly Client _supabase = supabase;
    private readonly string _supabaseUrl = settings.Value.Url;
    private readonly string _supabaseKey = settings.Value.PublishableKey;

    private IWebHostEnvironment _webHostEnvironment = web;

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var response = await _supabase.Auth.SignInWithPassword(request.Email, request.Password);
        return ExtractTokens(response);
    }

    public async Task LogoutAsync()
    {
        await _supabase.Auth.SignOut();
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // We make a bit of cheat here, supabase client prefers if we just refresh directly from the frontend client,
        // so we mimic how we'll do it from the frontend

        using var http = new HttpClient();

        http.DefaultRequestHeaders.Add("apikey", _supabaseKey);

        var body = new { refresh_token = request.RefreshToken };
        var response = await http.PostAsJsonAsync(
            $"{_supabaseUrl}/auth/v1/token?grant_type=refresh_token",
            body
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

        // This should happen since email confirmation is on (i.e. session is null
        // and user has to confirm their email)
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

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("apikey", _supabaseKey);

        var url = $"{_supabaseUrl}/auth/v1/recover?redirect_to={Uri.EscapeDataString(redirectUrl)}";
        var response = await http.PostAsJsonAsync(url, new { email = request.Email });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            // Same, log exception here but return JSON payload to client:
            Console.WriteLine($"Recovery email failed, with status {(int)response.StatusCode} {response.StatusCode}. Details: {error} ");

            throw new ExternalServiceException(
                error  
            );
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        using var verifyHttp = new HttpClient();
        verifyHttp.DefaultRequestHeaders.Add("apikey", _supabaseKey);

        var verifyResponse = await verifyHttp.PostAsJsonAsync(
            $"{_supabaseUrl}/auth/v1/verify",
            new { type = "recovery", token_hash = request.TokenHash }
        );

        if (!verifyResponse.IsSuccessStatusCode)
        {
            string error = await verifyResponse.Content.ReadAsStringAsync();
            // Reported error here instead, frontend expects a JSON error response:
            Console.WriteLine($"Recovery token invalid or expired. Status: {(int)verifyResponse.StatusCode}. Details: {error}");
            throw new ExternalServiceException(
               error
            );
        }

        SupabaseTokenResponse session =
            await verifyResponse.Content.ReadFromJsonAsync<SupabaseTokenResponse>()
            ?? throw new ExternalServiceException("Verify returned no session");

        using var updateHttp = new HttpClient();
        updateHttp.DefaultRequestHeaders.Add("apikey", _supabaseKey);
        updateHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            session.AccessToken
        );

        var updateResponse = await updateHttp.PutAsJsonAsync(
            $"{_supabaseUrl}/auth/v1/user",
            new { password = request.Password }
        );

        if (!updateResponse.IsSuccessStatusCode)
        {
            string error = await updateResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Password update failed. Status: {(int)updateResponse.StatusCode}. Details: {error}");
            throw new ExternalServiceException(
                error
            );
        }

        // Logs user out of all sessions, according to best practice

        using var logoutHttp = new HttpClient();
        logoutHttp.DefaultRequestHeaders.Add("apikey", _supabaseKey);
        logoutHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            session.AccessToken
        );

        var logoutResponse = await logoutHttp.PostAsync(
            $"{_supabaseUrl}/auth/v1/logout?scope=global",
            content: null
        );

        // Logging this only since password has already changed
        if (!logoutResponse.IsSuccessStatusCode)
        {
            string error = await verifyResponse.Content.ReadAsStringAsync();

            // TODO: Add proper logging for this
            Console.WriteLine($"Logout error: {error}");

            throw new ExternalServiceException(
                error
            );
        }
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
