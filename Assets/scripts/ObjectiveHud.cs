using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveHud : MonoBehaviour
{
    private const string DefaultMessage = "Procure a arma nas estantes com marcador verde";

    public static ObjectiveHud Instance { get; private set; }

    [SerializeField] private string objectiveMessage = DefaultMessage;
    [SerializeField] private Vector2 panelSize = new Vector2(620f, 58f);
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -28f);

    private GameObject panel;
    private TMP_Text messageText;

    public static ObjectiveHud GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject hudObject = new GameObject("ObjectiveHud", typeof(ObjectiveHud));
        return hudObject.GetComponent<ObjectiveHud>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        EnsureHud();
        ShowObjective(objectiveMessage);
    }

    public void ShowObjective(string message = DefaultMessage)
    {
        EnsureHud();

        if (messageText != null)
            messageText.text = message;

        if (panel != null)
            panel.SetActive(true);
    }

    public void HideObjective()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void EnsureHud()
    {
        if (panel != null && messageText != null)
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
            canvas = CreateCanvas();

        panel = new GameObject("ObjectiveHudPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = panelSize;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.68f);

        GameObject textObject = new GameObject("ObjectiveHudText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 8f);
        textRect.offsetMax = new Vector2(-18f, -8f);

        messageText = textObject.GetComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.white;
        messageText.fontSize = 24f;
        messageText.fontStyle = FontStyles.Bold;
        messageText.enableAutoSizing = true;
        messageText.fontSizeMin = 14f;
        messageText.fontSizeMax = 24f;
        messageText.raycastTarget = false;
        messageText.text = objectiveMessage;
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("ObjectiveHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }
}
