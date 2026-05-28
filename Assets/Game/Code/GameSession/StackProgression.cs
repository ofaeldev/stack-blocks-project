using UnityEngine;

public class StackProgression
{
    private const string BestScoreKey = "StackBlocks.BestScore";
    private const string TotalBlocksKey = "StackBlocks.TotalBlocks";
    private const string CoinsKey = "StackBlocks.Coins";

    public int BestScore { get; private set; }
    public int TotalBlocks { get; private set; }
    public int Coins { get; private set; }

    public int PlayerLevel => Mathf.Max(1, TotalBlocks / 25 + 1);
    public int UnlockedThemeCount => Mathf.Clamp(TotalBlocks / 20 + 1, 1, 4);
    public int UnlockedSkinCount => Mathf.Clamp(Coins / 250 + 1, 1, 6);

    public static StackProgression Load()
    {
        return new StackProgression
        {
            BestScore = PlayerPrefs.GetInt(BestScoreKey, 0),
            TotalBlocks = PlayerPrefs.GetInt(TotalBlocksKey, 0),
            Coins = PlayerPrefs.GetInt(CoinsKey, 0)
        };
    }

    public void RegisterRun(int score, int blocks)
    {
        BestScore = Mathf.Max(BestScore, score);
        TotalBlocks += blocks;
        Coins += Mathf.Max(0, score / 100 + blocks);

        PlayerPrefs.SetInt(BestScoreKey, BestScore);
        PlayerPrefs.SetInt(TotalBlocksKey, TotalBlocks);
        PlayerPrefs.SetInt(CoinsKey, Coins);
        PlayerPrefs.Save();
    }
}
