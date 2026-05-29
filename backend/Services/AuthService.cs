using Backend.DTOs;
using Backend.Exceptions;
using Backend.Settings;
using Microsoft.Extensions.Options;
using Supabase;

namespace Backend.Services;

public class AuthService(Client supabase, IOptions<SupabaseSettings> settings) : IAuthService
{
    private readonly Client _supabase = supabase;
    private readonly string _supabaseUrl = settings.Value.Url;
    private readonly string _supabaseKey = settings.Value.PublishableKey;

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
        return ValidateTokens(supabaseResponse == null ? null : new AuthResponse
        {
            AccessToken = supabaseResponse.AccessToken,
            RefreshToken = supabaseResponse.RefreshToken,
            ExpiresIn = supabaseResponse.ExpiresIn,
            TokenType = supabaseResponse.TokenType,
        });
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
