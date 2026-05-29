using UnityEngine;

public static class StackSkinLibrary
{
    public static readonly string[] SkinIds = { "classic", "neon", "glass", "gold" };
    public static readonly string[] SkinNames = { "Classic", "Neon", "Glass", "Gold" };
    public static readonly int[] SkinCosts = { 0, 150, 350, 700 };

    public static Color GetColor(string skinId)
    {
        return skinId switch
        {
            "neon" => new Color(0.95f, 0.08f, 1f),
            "glass" => new Color(0.35f, 0.95f, 1f),
            "gold" => new Color(1f, 0.72f, 0.18f),
            _ => new Color(1f, 0.35f, 0.85f)
        };
    }
}
