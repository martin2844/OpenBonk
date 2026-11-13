using System;
using System.Security.Cryptography;
using System.Text;

// Numeric adapter for the original game's enum-backed dictionaries. IDs are
// derived from canonical names, not load order; callers must reject collisions.
public static class OpenBonkContentIds
{
    public static int Character(string packageId, string contentId) => Make("character", packageId, contentId);
    public static int Weapon(string packageId, string contentId) => Make("weapon", packageId, contentId);
    public static int Item(string packageId, string contentId) => Make("item", packageId, contentId);
    public static int Map(string packageId, string contentId) => Make("map", packageId, contentId);
    private static int Make(string kind, string packageId, string contentId)
    {
        if (!Valid(packageId) || !Valid(contentId)) throw new ArgumentException("Content IDs require lowercase ASCII letters, digits, dot, underscore or hyphen");
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes("openbonk/v1/" + kind + "/" + packageId + "/" + contentId));
        uint number = ((uint)hash[0] << 24) | ((uint)hash[1] << 16) | ((uint)hash[2] << 8) | hash[3];
        return (int)(0x40000000u | (number & 0x0fffffffu));
    }
    private static bool Valid(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 64 || !AlphaNumeric(value[0])) return false;
        foreach (char c in value) if (!AlphaNumeric(c) && c != '.' && c != '_' && c != '-') return false;
        return true;
    }
    private static bool AlphaNumeric(char c) => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
}
