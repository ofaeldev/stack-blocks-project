using UnityEngine;

public class StackProgression
{
    private const string BestScoreKey = "StackBlocks.BestScore";
    private const string TotalBlocksKey = "StackBlocks.TotalBlocks";
    private const string CoinsKey = "StackBlocks.Coins";
    private const string SelectedSkinKey = "StackBlocks.SelectedSkin";
    private const string SkinUnlockPrefix = "StackBlocks.Skin.";

    public int BestScore { get; private set; }
    public int TotalBlocks { get; private set; }
    public int Coins { get; private set; }
    public string SelectedSkinId { get; private set; }

    public int PlayerLevel => Mathf.Max(1, TotalBlocks / 25 + 1);
    public int UnlockedThemeCount => Mathf.Clamp(TotalBlocks / 20 + 1, 1, 4);
    public int UnlockedSkinCount => Mathf.Clamp(Coins / 250 + 1, 1, 6);

    public static StackProgression Load()
    {
        return new StackProgression
        {
            BestScore = PlayerPrefs.GetInt(BestScoreKey, 0),
            TotalBlocks = PlayerPrefs.GetInt(TotalBlocksKey, 0),
            Coins = PlayerPrefs.GetInt(CoinsKey, 0),
            SelectedSkinId = PlayerPrefs.GetString(SelectedSkinKey, StackSkinLibrary.SkinIds[0])
        };
    }

    public void RegisterRun(int score, int blocks)
    {
        BestScore = Mathf.Max(BestScore, score);
        TotalBlocks += blocks;
        AddCoinsWithoutSaving(Mathf.Max(0, score / 100 + blocks));

        PlayerPrefs.SetInt(BestScoreKey, BestScore);
        PlayerPrefs.SetInt(TotalBlocksKey, TotalBlocks);
        PlayerPrefs.SetInt(CoinsKey, Coins);
        PlayerPrefs.Save();
    }

    public void AddCoins(int amount)
    {
        AddCoinsWithoutSaving(amount);
        PlayerPrefs.SetInt(CoinsKey, Coins);
        PlayerPrefs.Save();
    }

    public bool IsSkinUnlocked(string skinId)
    {
        if (skinId == StackSkinLibrary.SkinIds[0])
        {
            return true;
        }

        return PlayerPrefs.GetInt(SkinUnlockPrefix + skinId, 0) == 1;
    }

    public bool TryUnlockSkin(string skinId, int cost)
    {
        if (IsSkinUnlocked(skinId))
        {
            return true;
        }

        if (Coins < cost)
        {
            return false;
        }

        Coins -= cost;
        PlayerPrefs.SetInt(CoinsKey, Coins);
        PlayerPrefs.SetInt(SkinUnlockPrefix + skinId, 1);
        PlayerPrefs.Save();

        return true;
    }

    public void GrantSkin(string skinId)
    {
        PlayerPrefs.SetInt(SkinUnlockPrefix + skinId, 1);
        PlayerPrefs.Save();
    }

    public void SelectSkin(string skinId)
    {
        if (!IsSkinUnlocked(skinId))
        {
            return;
        }

        SelectedSkinId = skinId;
        PlayerPrefs.SetString(SelectedSkinKey, SelectedSkinId);
        PlayerPrefs.Save();
    }

    private void AddCoinsWithoutSaving(int amount)
    {
        Coins += Mathf.Max(0, amount);
    }
}
