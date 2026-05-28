using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StackMainMenu : MonoBehaviour
{
    private GameObject menuRoot;
    private Text titleText;
    private Text optionText;
    private Action<StackGameMode> onModeSelected;
    private bool isShowing;

    public static StackMainMenu Create()
    {
        GameObject menuObject = new("StackMainMenu");
        StackMainMenu menu = menuObject.AddComponent<StackMainMenu>();
        menu.Build();

        return menu;
    }

    private void Update()
    {
        if (!isShowing)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            SelectMode(StackGameMode.Relax);
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            SelectMode(StackGameMode.Hardcore);
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            SelectMode(StackGameMode.PhysicsMode);
        }
    }

    public void Show(Action<StackGameMode> selectedModeCallback)
    {
        onModeSelected = selectedModeCallback;
        isShowing = true;
        menuRoot.SetActive(true);
    }

    public void Hide()
    {
        isShowing = false;
        menuRoot.SetActive(false);
    }

    private void SelectMode(StackGameMode mode)
    {
        Hide();
        onModeSelected?.Invoke(mode);
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        menuRoot = new GameObject("MenuRoot");
        menuRoot.transform.SetParent(transform, false);

        Image background = menuRoot.AddComponent<Image>();
        background.color = new Color(0.03f, 0.04f, 0.08f, 0.88f);
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 32);

        titleText = CreateText("Title", menuRoot.transform, font, 76, TextAnchor.MiddleCenter);
        SetRect(titleText.rectTransform, new Vector2(0f, 205f), new Vector2(1000f, 110f), new Vector2(0.5f, 0.5f));
        titleText.text = "Stack Blocks";

        optionText = CreateText("Options", menuRoot.transform, font, 34, TextAnchor.MiddleCenter);
        SetRect(optionText.rectTransform, new Vector2(0f, -40f), new Vector2(1180f, 430f), new Vector2(0.5f, 0.5f));
        optionText.text =
            "1  Relax\n" +
            "Precise timing, clean tower, no physics collapse\n\n" +
            "2  Hardcore\n" +
            "One miss or unstable tower ends the run\n\n" +
            "3  Physics Mode\n" +
            "Arcade timing with living tower physics";

        menuRoot.SetActive(false);
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
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    private static void SetRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
    {
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }
}
