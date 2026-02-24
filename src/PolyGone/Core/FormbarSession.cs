namespace PolyGone;

/// <summary>
/// Holds the current Formbar login session for the logged-in player.
/// </summary>
public static class FormbarSession
{
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

    /// <summary>Clears all session data (logs the player out).</summary>
    public static void Clear()
    {
        ApiKey = "";
        UserId = 0;
        DisplayName = "";
        IsLoggedIn = false;
    }
}
