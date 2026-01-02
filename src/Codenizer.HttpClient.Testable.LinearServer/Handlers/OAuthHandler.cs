namespace Codenizer.HttpClient.Testable.LinearServer.Handlers
{
    /// <summary>
    /// Handles OAuth 2.0 token operations for the Linear API.
    /// </summary>
    public class OAuthHandler
    {
        private readonly LinearState _state;
        private readonly Dictionary<string, TokenInfo> _tokens = new();
        private readonly Dictionary<string, string> _authorizationCodes = new();

        public OAuthHandler(LinearState state)
        {
            _state = state;
        }

        /// <summary>
        /// Creates an authorization code for testing OAuth flows.
        /// </summary>
        public string CreateAuthorizationCode(string clientId)
        {
            var code = Guid.NewGuid().ToString("N");
            _authorizationCodes[code] = clientId;
            return code;
        }

        public object HandleTokenRequest(Dictionary<string, string> formData)
        {
            var grantType = formData.GetValueOrDefault("grant_type", "");

            return grantType switch
            {
                "authorization_code" => HandleAuthorizationCodeGrant(formData),
                "refresh_token" => HandleRefreshTokenGrant(formData),
                "client_credentials" => HandleClientCredentialsGrant(formData),
                _ => new { error = "unsupported_grant_type", error_description = $"Grant type '{grantType}' is not supported" }
            };
        }

        private object HandleAuthorizationCodeGrant(Dictionary<string, string> formData)
        {
            var code = formData.GetValueOrDefault("code", "");
            var clientId = formData.GetValueOrDefault("client_id", "");
            var clientSecret = formData.GetValueOrDefault("client_secret", "");
            var redirectUri = formData.GetValueOrDefault("redirect_uri", "");

            // Validate required parameters
            if (string.IsNullOrEmpty(code))
                return new { error = "invalid_request", error_description = "Missing required parameter: code" };
            if (string.IsNullOrEmpty(clientId))
                return new { error = "invalid_request", error_description = "Missing required parameter: client_id" };
            if (string.IsNullOrEmpty(clientSecret))
                return new { error = "invalid_request", error_description = "Missing required parameter: client_secret" };
            if (string.IsNullOrEmpty(redirectUri))
                return new { error = "invalid_request", error_description = "Missing required parameter: redirect_uri" };

            // For testing, accept any code that was pre-registered or generate tokens for test codes
            if (!_authorizationCodes.Remove(code) && !code.StartsWith("test_"))
            {
                return new { error = "invalid_grant", error_description = "Invalid authorization code" };
            }

            return GenerateTokenResponse(clientId);
        }

        private object HandleRefreshTokenGrant(Dictionary<string, string> formData)
        {
            var refreshToken = formData.GetValueOrDefault("refresh_token", "");
            var clientId = formData.GetValueOrDefault("client_id", "");
            var clientSecret = formData.GetValueOrDefault("client_secret", "");

            if (string.IsNullOrEmpty(refreshToken))
                return new { error = "invalid_request", error_description = "Missing required parameter: refresh_token" };

            // Find the token info by refresh token
            var tokenInfo = _tokens.Values.FirstOrDefault(t => t.RefreshToken == refreshToken);
            if (tokenInfo == null)
            {
                // For testing, accept any refresh token starting with 'lin_ref_'
                if (!refreshToken.StartsWith("lin_ref_"))
                {
                    return new { error = "invalid_grant", error_description = "Invalid refresh token" };
                }
                // Generate new tokens
                return GenerateTokenResponse(clientId);
            }

            // Revoke old tokens and generate new ones
            _tokens.Remove(tokenInfo.AccessToken);
            return GenerateTokenResponse(tokenInfo.ClientId);
        }

        private object HandleClientCredentialsGrant(Dictionary<string, string> formData)
        {
            var clientId = formData.GetValueOrDefault("client_id", "");
            var clientSecret = formData.GetValueOrDefault("client_secret", "");

            if (string.IsNullOrEmpty(clientId))
                return new { error = "invalid_request", error_description = "Missing required parameter: client_id" };
            if (string.IsNullOrEmpty(clientSecret))
                return new { error = "invalid_request", error_description = "Missing required parameter: client_secret" };

            return GenerateTokenResponse(clientId, actor: "app");
        }

        public object HandleRevokeRequest(Dictionary<string, string> formData, string? bearerToken)
        {
            // Token can be passed as form field or bearer token
            var accessToken = formData.GetValueOrDefault("access_token", "");
            var refreshToken = formData.GetValueOrDefault("refresh_token", "");

            if (!string.IsNullOrEmpty(accessToken))
            {
                _tokens.Remove(accessToken);
                return new { success = true };
            }

            if (!string.IsNullOrEmpty(refreshToken))
            {
                var tokenInfo = _tokens.Values.FirstOrDefault(t => t.RefreshToken == refreshToken);
                if (tokenInfo != null)
                {
                    _tokens.Remove(tokenInfo.AccessToken);
                }
                return new { success = true };
            }

            if (!string.IsNullOrEmpty(bearerToken))
            {
                _tokens.Remove(bearerToken);
                return new { success = true };
            }

            return new { success = true }; // Always return success for revoke
        }

        /// <summary>
        /// Validates an access token and returns true if valid.
        /// </summary>
        public bool ValidateToken(string accessToken)
        {
            // Accept tokens that exist in our store
            if (_tokens.ContainsKey(accessToken))
                return true;

            // Accept any token starting with 'lin_api_' or 'lin_oauth_' for testing
            return accessToken.StartsWith("lin_api_") || accessToken.StartsWith("lin_oauth_");
        }

        private object GenerateTokenResponse(string clientId, string actor = "user")
        {
            var accessToken = $"lin_oauth_{Guid.NewGuid():N}";
            var refreshToken = $"lin_ref_{Guid.NewGuid():N}";
            var expiresIn = 315360000; // 10 years in seconds (Linear tokens are long-lived)

            _tokens[accessToken] = new TokenInfo
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ClientId = clientId,
                Actor = actor,
                ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn)
            };

            return new
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = expiresIn,
                refresh_token = refreshToken,
                scope = "read,write"
            };
        }

        private class TokenInfo
        {
            public string AccessToken { get; set; } = "";
            public string RefreshToken { get; set; } = "";
            public string ClientId { get; set; } = "";
            public string Actor { get; set; } = "user";
            public DateTime ExpiresAt { get; set; }
        }
    }
}
