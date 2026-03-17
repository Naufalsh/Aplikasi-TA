using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class EventUI2 : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private RawImage eventImage;
    [SerializeField] private Button navigateButton;

    private EventDateu currentData;

    Coroutine loadImageCoroutine;
    private string currentImageUrl;

    void Start()
    {
        if (navigateButton != null)
            navigateButton.onClick.AddListener(OnNavigateClicked);
    }

    public void SetData(EventDateu data)
    {
        currentData = data;

        titleText.text = data.title;
        descriptionText.text = data.description;

        // reset image
        eventImage.texture = null;
        eventImage.color = Color.clear;

        if (string.IsNullOrEmpty(data.image))
            return;

        currentImageUrl = data.image;
        loadImageCoroutine = StartCoroutine(LoadImage(data.image));
    }

    IEnumerator LoadImage(string url)
    {
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();

            if (this == null || eventImage == null)
                yield break;

            if (url != currentImageUrl)
                yield break;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[EventUI2] Gagal load image: {req.error} | {url}");
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            eventImage.texture = tex;
            eventImage.color = Color.white;
        }
    }

    public void OnNavigateClicked()
    {
        Debug.Log("Navigate diklik → location: " + currentData.location);

        // Pathfinding
        if (RouteManager.Instance != null)
        {
            RouteManager.Instance.DrawRouteToBuilding(currentData.location);
        }
        else
        {
            Debug.LogError("RouteManager tidak ditemukan");
        }

        // Cari EventCarousel di scene lalu matikan
        GameObject carousel = GameObject.Find("EventCarousel");

        if (carousel != null)
        {
            carousel.SetActive(false);
            Debug.Log("EventCarousel ditutup");
        }
        else
        {
            Debug.LogWarning("EventCarousel tidak ditemukan di scene");
        }
    }

    private void OnDestroy()
    {
        if (loadImageCoroutine != null)
            StopCoroutine(loadImageCoroutine);
    }
}