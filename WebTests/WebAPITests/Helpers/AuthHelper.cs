using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WebAPITests.Models;

namespace WebAPITests.Helpers
{
    public class AuthHelper(HttpClient httpClient, IConfiguration configuration, ILogger<AuthHelper> logger) : IDisposable
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<AuthHelper> _logger = logger;
        private string? _cachedToken;
        private DateTime _tokenExpiry;

        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _cachedToken;
            }

            var tokenUrl = _configuration["ApiSettings:TokenUrl"];
            var clientId = _configuration["ApiSettings:ClientId"];
            var clientSecret = _configuration["ApiSettings:ClientSecret"];
            var scope = _configuration["ApiSettings:Scope"];
            var grantType = _configuration["ApiSettings:GrantType"];

            var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("client_Id", clientId!),
                new KeyValuePair<string, string>("client_Secret", clientSecret!),
                new KeyValuePair<string, string>("scope", scope!),
                new KeyValuePair<string, string>("grant_type", grantType!)
            ]);

            try
            {
                _logger.LogDebug("OAuth request: client_Id={ClientId}, scope={Scope}, grant_type=client_credentials", clientId, scope);
                var response = await _httpClient.PostAsync(tokenUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get access token. Status: {StatusCode}, Response: {Response}", response.StatusCode, responseBody);
                    throw new HttpRequestException($"Token request failed with status {response.StatusCode}: {responseBody}");
                }

                _logger.LogDebug($"Token response: {responseBody}");

                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    _logger.LogError("Invalid token response: {ResponseBody}", responseBody);
                    throw new InvalidOperationException("Failed to obtain access token: Invalid response");
                }

                _cachedToken = tokenResponse.AccessToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300); // 5-minute buffer

                _logger.LogInformation($"Successfully obtained new access token. Expires in {tokenResponse.ExpiresIn} seconds");

                _logger.LogDebug("Token type: {TokenType}, Scope: {Scope}", tokenResponse.TokenType, tokenResponse.Scope);
                return _cachedToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to obtain access token");
                throw;
            }
        }

        public HttpClient CreateAuthenticatedClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _cachedToken);
            client.BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"]!);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _httpClient?.Dispose();
        }
    }
}
