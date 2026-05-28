using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class StackMainMenu : MonoBehaviour
{
    private GameObject menuRoot;
    private GameObject confirmRoot;
    private Text titleText;
    private Text optionText;
    private Text confirmText;
    private Button relaxButton;
    private Button hardcoreButton;
    private Button physicsButton;
    private Button confirmExitButton;
    private Button continueButton;
    private Action<StackGameMode> onModeSelected;
    private Action onExitConfirmed;
    private Action onExitCancelled;
    private bool isShowing;
    private bool isConfirmingExit;

    public static StackMainMenu Create()
    {
        GameObject menuObject = new("StackMainMenu");
        StackMainMenu menu = menuObject.AddComponent<StackMainMenu>();
        menu.Build();

        return menu;
    }

    private void Update()
    {
        if (!isShowing && !isConfirmingExit)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (isConfirmingExit)
        {
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            {
                ConfirmExit();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelExit();
            }

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
        isConfirmingExit = false;
        menuRoot.SetActive(true);
        confirmRoot.SetActive(false);
    }

    public void ShowExitConfirmation(Action confirmedCallback, Action cancelledCallback)
    {
        onExitConfirmed = confirmedCallback;
        onExitCancelled = cancelledCallback;
        isShowing = false;
        isConfirmingExit = true;
        menuRoot.SetActive(false);
        confirmRoot.SetActive(true);
    }

    public void Hide()
    {
        isShowing = false;
        isConfirmingExit = false;
        menuRoot.SetActive(false);
        confirmRoot.SetActive(false);
    }

    private void SelectMode(StackGameMode mode)
    {
        Hide();
        onModeSelected?.Invoke(mode);
    }

    private void ConfirmExit()
    {
        Hide();
        onExitConfirmed?.Invoke();
    }

    private void CancelExit()
    {
        Hide();
        onExitCancelled?.Invoke();
    }

    private void Build()
    {
        EnsureEventSystem();

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
        SetRect(titleText.rectTransform, new Vector2(0f, 280f), new Vector2(1000f, 110f), new Vector2(0.5f, 0.5f));
        titleText.text = "Stack Blocks";

        optionText = CreateText("Options", menuRoot.transform, font, 28, TextAnchor.MiddleCenter);
        SetRect(optionText.rectTransform, new Vector2(0f, 185f), new Vector2(1180f, 80f), new Vector2(0.5f, 0.5f));
        optionText.text =
            "Choose a mode. Shop and unlocks will live here later.";

        relaxButton = CreateButton(
            "RelaxButton",
            menuRoot.transform,
            font,
            "Relax\nClean tower, no physics collapse",
            new Vector2(0f, 60f),
            () => SelectMode(StackGameMode.Relax)
        );

        hardcoreButton = CreateButton(
            "HardcoreButton",
            menuRoot.transform,
            font,
            "Hardcore\nOne miss or unstable tower ends the run",
            new Vector2(0f, -80f),
            () => SelectMode(StackGameMode.Hardcore)
        );

        physicsButton = CreateButton(
            "PhysicsButton",
            menuRoot.transform,
            font,
            "Physics Mode\nArcade timing with living tower physics",
            new Vector2(0f, -220f),
            () => SelectMode(StackGameMode.PhysicsMode)
        );

        confirmRoot = new GameObject("ConfirmExitRoot");
        confirmRoot.transform.SetParent(transform, false);

        Image confirmBackground = confirmRoot.AddComponent<Image>();
        confirmBackground.color = new Color(0.02f, 0.02f, 0.04f, 0.9f);
        RectTransform confirmBackgroundRect = confirmBackground.rectTransform;
        confirmBackgroundRect.anchorMin = Vector2.zero;
        confirmBackgroundRect.anchorMax = Vector2.one;
        confirmBackgroundRect.offsetMin = Vector2.zero;
        confirmBackgroundRect.offsetMax = Vector2.zero;

        confirmText = CreateText("ConfirmText", confirmRoot.transform, font, 46, TextAnchor.MiddleCenter);
        SetRect(confirmText.rectTransform, new Vector2(0f, 120f), new Vector2(1120f, 160f), new Vector2(0.5f, 0.5f));
        confirmText.text =
            "Exit current mode?\nYour current run will end.";

        confirmExitButton = CreateButton(
            "ConfirmExitButton",
            confirmRoot.transform,
            font,
            "Exit to Mode Menu",
            new Vector2(0f, -40f),
            ConfirmExit
        );

        continueButton = CreateButton(
            "ContinueButton",
            confirmRoot.transform,
            font,
            "Continue",
            new Vector2(0f, -180f),
            CancelExit
        );

        menuRoot.SetActive(false);
        confirmRoot.SetActive(false);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static Button CreateButton(string objectName, Transform parent, Font font, string label, Vector2 anchoredPosition, Action clicked)
    {
        GameObject buttonObject = new(objectName);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.16f, 0.24f, 0.96f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.12f, 0.16f, 0.24f, 0.96f);
        colors.highlightedColor = new Color(0.22f, 0.32f, 0.48f, 1f);
        colors.pressedColor = new Color(0.08f, 0.11f, 0.16f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.onClick.AddListener(() => clicked?.Invoke());

        SetRect(buttonObject.GetComponent<RectTransform>(), anchoredPosition, new Vector2(760f, 112f), new Vector2(0.5f, 0.5f));

        Text buttonText = CreateText("Label", buttonObject.transform, font, 30, TextAnchor.MiddleCenter);
        SetRect(buttonText.rectTransform, Vector2.zero, new Vector2(700f, 96f), new Vector2(0.5f, 0.5f));
        buttonText.text = label;

        return button;
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
