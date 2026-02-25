using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace PolyGone;

/// <summary>
/// Holds the current Formbar login session for the logged-in player.
/// </summary>
public static class FormbarSession
{
    private static readonly string SessionPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PolyGone", "session.json");

    /// <summary>Base URL of the Formbar server (e.g. "https://formbeta.yorktechapps.com").</summary>
    public static string ServerUrl { get; set; } = "https://formbeta.yorktechapps.com";

    /// <summary>
    /// The OAuth JWT token received from the Formbar Passport login.
    /// Stored here so it can be passed as auth for Digipog transfer API calls.
    /// </summary>
    public static string ApiKey { get; set; } = "";

    /// <summary>The player's Formbar user ID, populated after a successful login.</summary>
    public static int UserId { get; set; } = 0;

    /// <summary>The player's Formbar display name, populated after a successful login.</summary>
    public static string DisplayName { get; set; } = "";

    /// <summary>Whether the player is currently logged in.</summary>
    public static bool IsLoggedIn { get; set; } = false;

    /// <summary>
    /// The Formbar pool ID that receives Digipog payments for level unlocks.
    /// </summary>
    public const int GameAccountId = 47;

    /// <summary>
    /// Cost in Digipogs to unlock all levels at once.
    /// </summary>
    public const int LevelCost = 50;

    /// <summary>
    /// Purchase record key used to track whether the player has unlocked all levels.
    /// </summary>
    public const string AllLevelsKey = "all_levels";

    /// <summary>Maximum number of digits accepted for a Digipog PIN.</summary>
    public const int PinMaxLength = 8;

    /// <summary>
    /// Saves the current session to disk so the player is not asked to log in again
    /// next time the game starts.
    /// </summary>
    public static void SaveSession()
    {
        try
        {
            string? dir = Path.GetDirectoryName(SessionPath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var data = new { ServerUrl, ApiKey, UserId, DisplayName };
            File.WriteAllText(SessionPath, JsonSerializer.Serialize(data));
        }
        catch { }
    }

    /// <summary>
    /// Tries to restore a previously saved session.
    /// Returns <c>true</c> and populates all session fields on success.
    /// Returns <c>false</c> if no session file exists or the JWT has expired.
    /// </summary>
    public static bool TryLoadSession()
    {
        try
        {
            if (!File.Exists(SessionPath)) return false;

            string json = File.ReadAllText(SessionPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string apiKey = root.TryGetProperty("ApiKey", out var ak) ? ak.GetString() ?? "" : "";

            // Reject the saved session if the JWT has already expired
            if (IsJwtExpired(apiKey)) return false;

            ServerUrl = root.TryGetProperty("ServerUrl", out var su)
                ? su.GetString() ?? "https://formbeta.yorktechapps.com"
                : "https://formbeta.yorktechapps.com";
            ApiKey = apiKey;
            UserId = root.TryGetProperty("UserId", out var uid) ? uid.GetInt32() : 0;
            DisplayName = root.TryGetProperty("DisplayName", out var dn) ? dn.GetString() ?? "" : "";
            IsLoggedIn = true;
            return true;
        }
        catch { return false; }
    }

    /// <summary>Decodes the JWT exp claim and returns true if the token has expired.</summary>
    private static bool IsJwtExpired(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3) return true;
            string b64 = parts[1].Replace('-', '+').Replace('_', '/');
            int pad = (4 - b64.Length % 4) % 4;
            if (pad < 4) b64 += new string('=', pad);
            string payload = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("exp", out var exp))
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= exp.GetInt64();
            return false; // no exp claim – treat as non-expiring
        }
        catch { return true; }
    }

    /// <summary>Clears all session data and deletes the saved session file.</summary>
    public static void Clear()
    {
        ApiKey = "";
        UserId = 0;
        DisplayName = "";
        IsLoggedIn = false;
        try { if (File.Exists(SessionPath)) File.Delete(SessionPath); } catch { }
    }
}
