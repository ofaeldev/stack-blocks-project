using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StackHud : MonoBehaviour
{
    [SerializeField] private Color normalMessageColor = Color.white;
    [SerializeField] private Color comboMessageColor = new(0.2f, 1f, 0.45f);
    [SerializeField] private Color dangerMessageColor = new(1f, 0.25f, 0.15f);

    private Text scoreText;
    private Text comboText;
    private Text difficultyText;
    private Text metaText;
    private Text messageText;
    private Coroutine messageRoutine;

    public static StackHud Create()
    {
        GameObject hudObject = new("StackHud");
        StackHud hud = hudObject.AddComponent<StackHud>();
        hud.Build();

        return hud;
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 32);

        scoreText = CreateText("ScoreText", transform, font, 34, TextAnchor.UpperLeft);
        SetRect(scoreText.rectTransform, new Vector2(28f, -24f), new Vector2(520f, 70f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        comboText = CreateText("ComboText", transform, font, 30, TextAnchor.UpperLeft);
        SetRect(comboText.rectTransform, new Vector2(28f, -82f), new Vector2(520f, 60f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        difficultyText = CreateText("DifficultyText", transform, font, 24, TextAnchor.UpperRight);
        SetRect(difficultyText.rectTransform, new Vector2(-28f, -28f), new Vector2(680f, 110f), new Vector2(1f, 1f), new Vector2(1f, 1f));

        metaText = CreateText("MetaText", transform, font, 22, TextAnchor.LowerLeft);
        SetRect(metaText.rectTransform, new Vector2(28f, 24f), new Vector2(760f, 80f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        messageText = CreateText("MessageText", transform, font, 56, TextAnchor.MiddleCenter);
        SetRect(messageText.rectTransform, Vector2.zero, new Vector2(900f, 140f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f));
        messageText.color = Color.clear;
    }

    public void SetStats(int blocks, int score, int combo, int perfectStreak, float speed, float tolerance, string modeName, float balanceRisk, string biomeName)
    {
        scoreText.text = $"Score {score}\nBlocks {blocks}";
        string comboLine = combo > 1 ? $"Combo x{combo}" : "Combo x1";
        string perfectLine = perfectStreak > 0 ? $"\nPerfect x{perfectStreak}" : string.Empty;
        comboText.text = comboLine + perfectLine;
        comboText.color = combo > 1 || perfectStreak > 0 ? comboMessageColor : normalMessageColor;
        difficultyText.text = $"{modeName}\nSpeed {speed:0.0}\nTolerance {tolerance:0.00}\nBalance {balanceRisk:0%}\n{biomeName}";
    }

    public void SetMeta(StackProgression progression)
    {
        metaText.text = $"Best {progression.BestScore} | Coins {progression.Coins} | Level {progression.PlayerLevel} | Skins {progression.UnlockedSkinCount} | Themes {progression.UnlockedThemeCount}";
    }

    public void ShowPlacement(bool wasCombo, int gainedScore, bool isPerfect, string precisionLabel)
    {
        string comboPart = wasCombo ? " Combo!" : "";
        string message = isPerfect ? $"PERFECT +{gainedScore}{comboPart}" : $"{precisionLabel} +{gainedScore}{comboPart}";
        Color color = isPerfect || wasCombo ? comboMessageColor : normalMessageColor;
        float duration = isPerfect ? 0.9f : 0.65f;
        ShowMessage(message, color, duration);
    }

    public void ShowDanger(string message)
    {
        ShowMessage(message, dangerMessageColor, 1.1f);
    }

    public void ShowReady()
    {
        ShowMessage("Stack!", normalMessageColor, 0.9f);
    }

    private void ShowMessage(string message, Color color, float duration)
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }

        messageRoutine = StartCoroutine(MessageRoutine(message, color, duration));
    }

    private IEnumerator MessageRoutine(string message, Color color, float duration)
    {
        messageText.text = message;
        messageText.color = color;
        messageText.transform.localScale = Vector3.one * 1.08f;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float alpha = Mathf.Lerp(1f, 0f, progress);
            messageText.color = new Color(color.r, color.g, color.b, alpha);
            messageText.transform.localScale = Vector3.Lerp(Vector3.one * 1.08f, Vector3.one, progress);
            yield return null;
        }

        messageText.color = Color.clear;
        messageRoutine = null;
    }

    private static Text CreateText(string objectName, Transform parent, Font font, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    private static void SetRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size, Vector2 anchor, Vector2 pivot)
    {
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }
}
