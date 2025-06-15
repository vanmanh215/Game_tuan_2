using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RectTransform logoRect;
    [SerializeField] private CanvasGroup buttonsCanvasGroup;

    [Header("UI Panels")]
    [SerializeField] private GameObject helpPanel;

    [Header("Animation Settings")]
    [SerializeField] private float logoDropDuration = 1.5f;
    [SerializeField] private float buttonsFadeDuration = 1f;

    [Header("Scene To Load")]
    [SerializeField] private string gameSceneName = "Game";

    void Start()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
        }

        logoRect.anchoredPosition = new Vector2(0, 800f);
        buttonsCanvasGroup.alpha = 0;

        AnimateEntry();
    }

    private void AnimateEntry()
    {
        Sequence entrySequence = DOTween.Sequence();
        entrySequence.Append(logoRect.DOAnchorPosY(200f, logoDropDuration).SetEase(Ease.OutBounce));
        entrySequence.Append(buttonsCanvasGroup.DOFade(1, buttonsFadeDuration));
    }

    public void PlayGame()
    {
        buttonsCanvasGroup.interactable = false;
        transform.DOScale(0.95f, 0.3f).OnComplete(() =>
        {
            SceneManager.LoadScene(gameSceneName);
        });
    }

    public void ToggleHelpPanel()
    {
        if (helpPanel == null) return;

        // Bật/tắt panel
        bool isActive = !helpPanel.activeSelf;
        helpPanel.SetActive(isActive);

        buttonsCanvasGroup.interactable = !isActive;
    }
}