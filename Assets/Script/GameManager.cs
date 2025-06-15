using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public enum GameState
{
    Playing,
    LevelComplete,
    GameOver
}
public class GameManager : MonoBehaviour
{
    public GameState currentState;

    [Header("Level Management")]
    [SerializeField] private List<GameObject> levelPrefabs;
    private int currentLevelIndex = 0;
    private GameObject currentLevelInstance;

    [Header("In-Game Objects")]
    private Transform levelContainer;

    [Header("Level State")]
    private int itemsToCollect = 0;
    private int itemsCollected = 0;
    private Vector3Int holePosition;
    private bool holeIsFound = false;

    [Header("UI Panels")]
    [SerializeField] private GameObject allLevelsCompletePanel;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        currentLevelIndex = PlayerPrefs.GetInt("CurrentLevel", 0);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu") return; // Bỏ qua nếu là scene MainMenu

        // Bước 1: Yêu cầu UIManager tìm và gán các button của nó trước.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.FindAndAssignButtons();
        }
        // Bước 2: SAU KHI UIManager đã sẵn sàng, GameManager mới bắt đầu gán sự kiện.
        AssignHUDButtons();

        // Bước 3: Tìm Level Container và tải Level.
        var containerObject = GameObject.FindWithTag("LevelContainer");
        if (containerObject != null) levelContainer = containerObject.transform;
        else Debug.LogError("FATAL ERROR: Could not find GameObject with tag 'LevelContainer'!");

        if (allLevelsCompletePanel != null) allLevelsCompletePanel.SetActive(false);
        LoadLevel();
    }
    void AssignHUDButtons()
    {
        if (UIManager.Instance == null || UIManager.Instance.homeButton == null || UIManager.Instance.retryButton == null)
        {
            Debug.LogWarning("UIManager or its buttons are not ready, cannot assign HUD buttons.");
            return;
        }

        UIManager.Instance.homeButton.onClick.RemoveAllListeners();
        UIManager.Instance.retryButton.onClick.RemoveAllListeners();

        UIManager.Instance.homeButton.onClick.AddListener(GoToMainMenu);
        UIManager.Instance.retryButton.onClick.AddListener(RetryLevel);
    }

    void LoadLevel()
    {
        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
        }

        if (currentLevelIndex >= levelPrefabs.Count)
        {
            if (allLevelsCompletePanel != null) allLevelsCompletePanel.SetActive(true);
            return;
        }

        GameObject levelToLoad = levelPrefabs[currentLevelIndex];
        currentLevelInstance = Instantiate(levelToLoad, levelContainer);

        // 1. Lấy "tờ khai" LevelData từ prefab vừa tạo
        LevelData levelData = currentLevelInstance.GetComponent<LevelData>();

        if (levelData != null)
        {
            // 2. Đưa các tham chiếu từ "tờ khai" cho GridManager
            GridManager.Instance.SetTilemaps(levelData.groundTilemap, levelData.itemsTilemap, levelData.snakeTilemap);

            // 3. Đếm vật phẩm (bây giờ sẽ chạy đúng)
            currentState = GameState.Playing;
            CountCollectiblesAndFindHole();

            // 4. Kích hoạt Player (lấy từ "tờ khai")
            if (levelData.playerController != null)
            {
                InputManager.Instance.RegisterPlayer(levelData.playerController);
                levelData.playerController.ActivatePlayer();

            }
            else
            {
                Debug.LogError("PlayerController not assigned in LevelData for prefab: " + currentLevelInstance.name);
            }
        }
        else
        {
            Debug.LogError("FATAL: The loaded level prefab '" + levelToLoad.name + "' is missing a LevelData component!");
        }
    }
    public void WinGame()
    {
        if (currentState != GameState.Playing) return;
        currentState = GameState.LevelComplete;
        Debug.Log("LEVEL " + (currentLevelIndex + 1) + " COMPLETE!");

        currentLevelIndex++;
        PlayerPrefs.SetInt("CurrentLevel", currentLevelIndex);
        PlayerPrefs.Save();

        // Dùng DOTween để tạo độ trễ 1 giây trước khi tải màn mới
        DOVirtual.DelayedCall(1f, () => {
            LoadNextLevel();
        }).SetUpdate(true);
    }

    public void LoadNextLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void RetryLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    private void CountCollectiblesAndFindHole()
    {
        itemsToCollect = 0;
        holeIsFound = false;
        Tilemap itemsMap = GridManager.Instance.ItemsTilemap;
        if (itemsMap == null)
        {
            Debug.LogError("Cannot count items because ItemsTilemap is null in GridManager!");
            return;
        }
        BoundsInt bounds = itemsMap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                TileType type = GridManager.Instance.GetTileTypeAt(pos);
                if (type == TileType.Banana || type == TileType.Chili)
                {
                    itemsToCollect++;
                }
                else if (type == TileType.HoleClosed)
                {
                    holePosition = pos;
                    holeIsFound = true;
                }
            }
        }
    }

    public void OnItemCollected()
    {
        if (currentState != GameState.Playing) return;
        itemsCollected++;
        if (itemsCollected >= itemsToCollect)
        {
            if (holeIsFound)
            {
                GridManager.Instance.OpenHoleTile(holePosition);
            }
        }
    }

    //public void LoseGame(string reason)
    //{
    //    if (currentState != GameState.Playing) return;
    //    currentState = GameState.GameOver;
    //    Debug.Log("GAME OVER: " + reason);
    //    // Rắn sẽ tự dừng lại do check 'currentState' trong Update
    //    // Người chơi sẽ tự bấm nút Retry hoặc Home trên màn hình
    //}
}