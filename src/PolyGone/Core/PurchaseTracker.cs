using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PolyGone;

/// <summary>
/// Tracks which levels each Formbar user has already purchased so that they
/// are not charged more than once per level.  Data is persisted to a local
/// JSON file in the user's application-data folder.
///
/// Each record is HMAC-signed with a key derived from the user ID so that
/// manually editing the file does not grant free level access.
/// </summary>
public static class PurchaseTracker
{
    private static readonly string SavePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PolyGone",
        "purchases.json");

    // userId -> { levelName -> HMAC signature }
    private static Dictionary<int, Dictionary<string, string>> _purchases = new();

    /// <summary>Loads purchase data from disk.  Safe to call multiple times.</summary>
    public static void Load()
    {
        _purchases = new Dictionary<int, Dictionary<string, string>>();
        try
        {
            if (!File.Exists(SavePath)) return;

            string json = File.ReadAllText(SavePath);
            var raw = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
            if (raw == null) return;

            foreach (var kvp in raw)
            {
                if (int.TryParse(kvp.Key, out int uid))
                    _purchases[uid] = kvp.Value ?? new Dictionary<string, string>();
            }
        }
        catch { }
    }

    /// <summary>
    /// Returns true if the specified user has a valid (unmodified) purchase record
    /// for the given level.
    /// </summary>
    public static bool HasPurchased(int userId, string levelName)
    {
        if (!_purchases.TryGetValue(userId, out var levels)) return false;
        if (!levels.TryGetValue(levelName, out string? storedSig)) return false;
        return storedSig == ComputeSignature(userId, levelName);
    }

    /// <summary>Records that a user has purchased access to a level, then saves to disk.</summary>
    public static void RecordPurchase(int userId, string levelName)
    {
        if (!_purchases.TryGetValue(userId, out var levels))
        {
            levels = new Dictionary<string, string>();
            _purchases[userId] = levels;
        }
        levels[levelName] = ComputeSignature(userId, levelName);
        Save();
    }

    /// <summary>
    /// Computes an HMAC-SHA256 signature for a (userId, levelName) pair using a
    /// key derived from the user ID.  This deters trivial JSON edits while keeping
    /// the implementation self-contained.
    /// </summary>
    private static string ComputeSignature(int userId, string levelName)
    {
        byte[] key = Encoding.UTF8.GetBytes($"PolyGone-Purchases-{userId}-v1");
        using var hmac = new HMACSHA256(key);
        byte[] data = Encoding.UTF8.GetBytes($"{userId}:{levelName}");
        return Convert.ToBase64String(hmac.ComputeHash(data));
    }

    private static void Save()
    {
        try
        {
            string? dir = Path.GetDirectoryName(SavePath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var raw = new Dictionary<string, Dictionary<string, string>>();
            foreach (var kvp in _purchases)
                raw[kvp.Key.ToString()] = kvp.Value;

            File.WriteAllText(SavePath, JsonSerializer.Serialize(raw));
        }
        catch { }
    }
}

