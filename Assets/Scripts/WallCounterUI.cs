using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WallCounterUI : MonoBehaviour
{
    private static WallCounterUI instance;

    [SerializeField] private TextMeshProUGUI counterText;
    private Canvas rootCanvas;

    private int totalWalls;
    private int brokenWalls;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateCounter()
    {
        if (instance != null || FindObjectOfType<WallCounterUI>() != null)
        {
            return;
        }

        var go = new GameObject("WallCounter");
        instance = go.AddComponent<WallCounterUI>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUI();
    }

    private void OnEnable()
    {
        SimpleBreakableWall.WallBroken += HandleWallBroken;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        CountExistingWalls();
        UpdateCounter();
        ApplyVisibilityForScene();
    }

    private void OnDisable()
    {
        SimpleBreakableWall.WallBroken -= HandleWallBroken;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void EnsureUI()
    {
        if (counterText != null)
        {
            return;
        }

        // Create a lightweight overlay canvas and text element if none are assigned.
        var canvasGO = new GameObject("WallCounterCanvas");
        rootCanvas = canvasGO.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = rootCanvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        var textGO = new GameObject("WallCounterText");
        textGO.transform.SetParent(canvasGO.transform, false);

        counterText = textGO.AddComponent<TextMeshProUGUI>();
        counterText.fontSize = 36f;
        counterText.alignment = TextAlignmentOptions.TopLeft;
        counterText.color = Color.white;

        var rect = counterText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
    }

    private void CountExistingWalls()
    {
        SimpleBreakableWall[] walls = FindObjectsOfType<SimpleBreakableWall>();
        totalWalls = walls.Length;
        brokenWalls = 0;

        foreach (var wall in walls)
        {
            if (wall.HasBroken)
            {
                brokenWalls++;
            }
        }
    }

    private void HandleWallBroken(SimpleBreakableWall _)
    {
        brokenWalls++;
        UpdateCounter();
        ApplyVisibilityForScene();
    }

    private void HandleSceneLoaded(Scene _, LoadSceneMode __)
    {
        CountExistingWalls();
        UpdateCounter();
        ApplyVisibilityForScene();
    }

    private void UpdateCounter()
    {
        if (counterText == null)
        {
            return;
        }

        counterText.text = $"Walls Broken: {brokenWalls}/{totalWalls}";
    }

    private void ApplyVisibilityForScene()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInChildren<Canvas>();
        }

        if (rootCanvas != null)
        {
            rootCanvas.gameObject.SetActive(totalWalls > 0);
        }
    }
}
