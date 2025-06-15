using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public Button upButton { get; private set; }
    public Button downButton { get; private set; }
    public Button leftButton { get; private set; }
    public Button rightButton { get; private set; }
    public Button homeButton { get; private set; }
    public Button retryButton { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void FindAndAssignButtons()
    {
        GameObject uiCanvas = GameObject.FindWithTag("InGameUICanvas");
        if (uiCanvas == null)
        {
            return;
        }
        Transform movementButtons = uiCanvas.transform.Find("MovementButtons");
        if (movementButtons != null)
        {
            upButton = movementButtons.Find("UpButton")?.GetComponent<Button>();
            downButton = movementButtons.Find("DownButton")?.GetComponent<Button>();
            leftButton = movementButtons.Find("LeftButton")?.GetComponent<Button>();
            rightButton = movementButtons.Find("RightButton")?.GetComponent<Button>();
        }

        Transform topRightButtons = uiCanvas.transform.Find("TopRight_Buttons");
        if (topRightButtons != null)
        {
            homeButton = topRightButtons.Find("HomeButton")?.GetComponent<Button>();
            retryButton = topRightButtons.Find("RetryButton")?.GetComponent<Button>();
        }

        if (upButton == null || homeButton == null)
        {
            Debug.LogError("UIManager failed to find one or more buttons! Check names and hierarchy paths inside InGameUI_Canvas.");
        }
        else
        {
            Debug.Log("UIManager successfully found and assigned all HUD buttons.");
        }
    }
}