using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WallCounterUI : MonoBehaviour
{
    private static bool hasSpawned;

    [SerializeField] private TextMeshProUGUI counterText;

    private int totalWalls;
    private int brokenWalls;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateCounter()
    {
        if (hasSpawned || FindObjectOfType<WallCounterUI>() != null)
        {
            return;
        }

        var go = new GameObject("WallCounter");
        go.AddComponent<WallCounterUI>();
        hasSpawned = true;
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject); //makes sure ui stays when game reloaded after death
        EnsureUI();
    }

    private void OnEnable()
    {
        SimpleBreakableWall.WallBroken += HandleWallBroken;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LoseScreen") //hide the counter during death screen
        {
            // Hide UI entirely
            SetUIVisible(false);
            return;
        }

        // Show UI for gameplay
        SetUIVisible(true);

        brokenWalls = 0;
        CountExistingWalls();
        UpdateCounter();
    }

    private void SetUIVisible(bool visible)
{
    if (counterText != null)
    {
        counterText.gameObject.SetActive(visible);
    }

    if (counterText != null && counterText.transform.parent != null)
    {
        counterText.transform.parent.gameObject.SetActive(visible);
    }
}

    private void Start()
    {
        CountExistingWalls();
        UpdateCounter();
    }

    private void OnDisable()
    {
        SimpleBreakableWall.WallBroken -= HandleWallBroken;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void EnsureUI()
    {
        if (counterText != null)
        {
            return;
        }

        // Create a lightweight overlay canvas and text element if none are assigned.
        var canvasGO = new GameObject("WallCounterCanvas");
        DontDestroyOnLoad(canvasGO);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
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
    }

    private void UpdateCounter()
    {
        if (counterText == null)
        {
            return;
        }

        counterText.text = $"Walls Broken: {brokenWalls}/{totalWalls}";
    }
}
