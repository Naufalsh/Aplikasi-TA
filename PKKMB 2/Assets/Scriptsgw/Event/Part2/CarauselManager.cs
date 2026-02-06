using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarauselManager : MonoBehaviour
{
    [Header("Navigation Dots")]
    public GameObject dotsContainer;
    public GameObject dotPrefab;

    [Header("Buttons")]
    public Button nextButton;
    public Button prevButton;

    [Header("Settings")]
    public bool useTimer = true;
    public bool isLimitedSwipe = false;
    public float autoMoveTime = 5f;
    public float swipeThreshold = 50f;

    public RectTransform contentArea;

    [HideInInspector]
    public List<GameObject> contentPanels = new List<GameObject>();

    private int currentIndex = 0;
    private float timer;
    private Vector2 touchStartPos;

    void Start()
    {
        nextButton.onClick.AddListener(NextContent);
        prevButton.onClick.AddListener(PreviousContent);
    }

    public void SetContents(List<GameObject> panels)
    {
        contentPanels = panels;
        currentIndex = 0;

        // 🔥 PAKSA SEMUA PANEL AKTIF
        foreach (GameObject panel in contentPanels)
        {
            panel.SetActive(true);

            // pastikan punya CanvasGroup
            if (!panel.TryGetComponent(out CanvasGroup cg))
                cg = panel.AddComponent<CanvasGroup>();

            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        foreach (Transform child in dotsContainer.transform)
            Destroy(child.gameObject);

        InitializeDots();

        // ⏱️ TUNDA MODE CAROUSEL
        StartCoroutine(DelayedCarouselStart());

        if (useTimer)
        {
            timer = autoMoveTime;
            CancelInvoke();
            InvokeRepeating(nameof(AutoMoveContent), 1f, 1f);
        }
    }

    IEnumerator DelayedCarouselStart()
    {
        // 🔥 Kasih waktu image load
        yield return new WaitForSeconds(2f);

        ShowContent();
    }

    void InitializeDots()
    {
        for (int i = 0; i < contentPanels.Count; i++)
        {
            GameObject dot = Instantiate(dotPrefab, dotsContainer.transform);
            Image img = dot.GetComponent<Image>();
            img.fillAmount = 0f;
            img.color = (i == currentIndex) ? Color.white : Color.gray;
        }
    }

    void Update()
    {
        DetectSwipe();
    }

    void DetectSwipe()
    {
        if (Input.GetMouseButtonDown(0))
            touchStartPos = Input.mousePosition;

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 endPos = Input.mousePosition;
            float distance = endPos.x - touchStartPos.x;

            if (Mathf.Abs(distance) < swipeThreshold)
                return;

            if (!RectTransformUtility.RectangleContainsScreenPoint(contentArea, touchStartPos))
                return;

            if (distance > 0)
                PreviousContent();
            else
                NextContent();
        }
    }

    void AutoMoveContent()
    {
        timer -= 1f;

        if (timer <= 0)
        {
            timer = autoMoveTime;
            NextContent();
        }

        UpdateDots();
    }

    void UpdateDots()
    {
        for (int i = 0; i < dotsContainer.transform.childCount; i++)
        {
            Image img = dotsContainer.transform.GetChild(i).GetComponent<Image>();

            img.fillAmount = (i == currentIndex) ? timer / autoMoveTime : 0f;
            img.color = (i == currentIndex) ? Color.white : Color.gray;
        }
    }

    void NextContent()
    {
        if (contentPanels.Count == 0) return;

        currentIndex = (currentIndex + 1) % contentPanels.Count;
        ShowContent();
    }

    void PreviousContent()
    {
        if (contentPanels.Count == 0) return;

        currentIndex = (currentIndex - 1 + contentPanels.Count) % contentPanels.Count;
        ShowContent();
    }

    void ShowContent()
    {
        for (int i = 0; i < contentPanels.Count; i++)
        {
            CanvasGroup cg = contentPanels[i].GetComponent<CanvasGroup>();

            bool isActive = (i == currentIndex);

            cg.alpha = isActive ? 1f : 0f;
            cg.blocksRaycasts = isActive;
            cg.interactable = isActive;
        }

        timer = autoMoveTime;
        UpdateDots();
    }
}
