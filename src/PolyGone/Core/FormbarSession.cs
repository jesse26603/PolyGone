namespace PolyGone;

/// <summary>
/// Holds the current Formbar login session for the logged-in player.
/// </summary>
public static class FormbarSession
{
    /// <summary>Base URL of the Formbar server (e.g. "https://formbeta.yorktechapps.com").</summary>
    public static string ServerUrl { get; set; } = "https://formbeta.yorktechapps.com";

    /// <summary>The player's Formbar API key (found at /profile on the Formbar server).</summary>
    public static string ApiKey { get; set; } = "";

    /// <summary>The player's Formbar user ID, populated after a successful login.</summary>
    public static int UserId { get; set; } = 0;

    /// <summary>The player's Formbar display name, populated after a successful login.</summary>
    public static string DisplayName { get; set; } = "";

    /// <summary>The player's current Digipog balance, populated after a successful login.</summary>
    public static int Digipogs { get; set; } = 0;

    /// <summary>Whether the player is currently logged in.</summary>
    public static bool IsLoggedIn { get; set; } = false;

    /// <summary>
    /// The Formbar user ID of the game's account that receives Digipog payments.
    /// Change this to the actual game/teacher account ID before deployment.
    /// </summary>
    public const int GameAccountId = 0;

    /// <summary>Cost in Digipogs to unlock each level.</summary>
    public const int LevelCost = 5;

    /// <summary>Maximum number of digits accepted for a Digipog PIN.</summary>
    public const int PinMaxLength = 8;

    /// <summary>Clears all session data (logs the player out).</summary>
    public static void Clear()
    {
        ApiKey = "";
        UserId = 0;
        DisplayName = "";
        Digipogs = 0;
        IsLoggedIn = false;
    }
}
