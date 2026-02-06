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


    Coroutine loadImageCoroutine;

    private string currentImageUrl;

    public void SetData(EventDateu data)
    {
        titleText.text = data.title;
        descriptionText.text = data.description;

        // Reset image setiap SetData
        eventImage.texture = null;
        eventImage.color = Color.clear;

        if (string.IsNullOrEmpty(data.image))
            return;

        currentImageUrl = data.image;

        // ❌ JANGAN stop coroutine lama
        // ✔️ Biarkan coroutine lama selesai tapi dicek URL-nya
        loadImageCoroutine = StartCoroutine(LoadImage(data.image));
    }

    IEnumerator LoadImage(string url)
    {
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();

            // Panel sudah dihancurkan
            if (this == null || eventImage == null)
                yield break;

            // Data sudah berubah (panel dipakai ulang)
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

            Debug.Log($"[EventUI2] Image loaded OK → {gameObject.name}");
        }
    }

    private void OnDestroy()
    {
        if (loadImageCoroutine != null)
            StopCoroutine(loadImageCoroutine);
    }
}


