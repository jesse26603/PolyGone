using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PolyGone;

/// <summary>
/// Provides async wrappers around the Formbar HTTP API used for login verification
/// and Digipog payment processing.
/// </summary>
public static class FormbarService
{
    private static readonly HttpClient _client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public record UserInfo(int Id, string DisplayName, int Digipogs);
    public record TransferResult(bool Success, string Message);

    /// <summary>
    /// Decodes a Formbar Passport JWT token and returns the user's info.
    /// The JWT payload is base64url-encoded JSON – no API call is required.
    /// Returns null if the token is malformed.
    /// </summary>
    public static UserInfo? DecodeJwt(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3) return null;

            // Base64url → Base64 → bytes → JSON string
            string base64 = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');
            int pad = (4 - base64.Length % 4) % 4;
            if (pad < 4) base64 += new string('=', pad);

            byte[] bytes = Convert.FromBase64String(base64);
            string json = Encoding.UTF8.GetString(bytes);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            int id = ReadInt(root, "id");
            string displayName = root.TryGetProperty("displayName", out var dn)
                ? dn.GetString() ?? "" : "";
            int digipogs = ReadInt(root, "digipogs");

            return new UserInfo(id, displayName, digipogs);
        }
        catch
        {
            return null;
        }
    }

    private static int ReadInt(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var prop)) return 0;
        if (prop.ValueKind != JsonValueKind.Number) return 0;
        // Use GetDouble() then cast so fractional values like 10.0 are handled correctly
        return (int)prop.GetDouble();
    }

    /// <summary>
    /// Calls GET /api/me to verify an API key and retrieve the user's profile.
    /// Returns (UserInfo, null) on success or (null, errorMessage) on failure.
    /// </summary>
    public static async Task<(UserInfo? user, string? error)> GetCurrentUser(string serverUrl, string apiKey)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{serverUrl.TrimEnd('/')}/api/me");
            request.Headers.TryAddWithoutValidation("API", apiKey);

            var response = await _client.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!response.IsSuccessStatusCode)
            {
                string errMsg = root.TryGetProperty("error", out var e)
                    ? e.GetString() ?? "Unknown error"
                    : $"HTTP {(int)response.StatusCode}";
                return (null, errMsg);
            }

            int id = root.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
            string displayName = root.TryGetProperty("displayName", out var dn)
                ? dn.GetString() ?? ""
                : "";
            int digipogs = root.TryGetProperty("digipogs", out var dg) ? dg.GetInt32() : 0;

            return (new UserInfo(id, displayName, digipogs), null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Calls POST /api/digipogs/transfer to charge the player for level access.
    /// The player's PIN (from their /profile page) is required to authorise the transfer.
    /// </summary>
    public static async Task<TransferResult> TransferDigipogs(
        string serverUrl, string apiKey,
        int fromId, int toId,
        int amount, string reason,
        int pin)
    {
        try
        {
            var payload = new { from = fromId, to = toId, amount, reason, pin, pool = true };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{serverUrl.TrimEnd('/')}/api/digipogs/transfer");
            if (!string.IsNullOrEmpty(apiKey))
                request.Headers.TryAddWithoutValidation("API", apiKey);
            request.Content = content;

            var response = await _client.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            bool success = root.TryGetProperty("success", out var s) && s.GetBoolean();
            string message = root.TryGetProperty("message", out var m)
                ? m.GetString() ?? ""
                : "";

            return new TransferResult(success, message);
        }
        catch (Exception ex)
        {
            return new TransferResult(false, ex.Message);
        }
    }
}
