using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.Networking;
using System.Collections;

[System.Serializable]
public class EventData
{
    public string title;
    public string description;
    public string image;
}

[System.Serializable]
public class EventWrapper
{
    public EventData[] events;
}

public class EventManager2 : MonoBehaviour
{
    public TextMeshProUGUI judulEvent;
    public TextMeshProUGUI deskripsiEvent;

    // ⬇️ DIUBAH: Image → RawImage
    public RawImage imageEvent;

    void Start()
    {
        PlayFabClientAPI.GetTitleData(
            new GetTitleDataRequest(),
            OnSuccess,
            OnError
        );
    }

    void OnSuccess(GetTitleDataResult result)
    {
        if (result.Data == null || !result.Data.ContainsKey("Events"))
        {
            Debug.LogError("Key 'Events' tidak ada di PlayFab");
            return;
        }

        Debug.Log("RAW Events JSON = " + result.Data["Events"]);

        string wrapped = "{\"events\":" + result.Data["Events"] + "}";
        EventWrapper wrapper = JsonUtility.FromJson<EventWrapper>(wrapped);

        if (wrapper.events == null || wrapper.events.Length == 0)
        {
            Debug.LogError("EVENT ARRAY KOSONG");
            return;
        }

        EventData e = wrapper.events[0];

        Debug.Log("IMAGE URL FROM PLAYFAB = " + e.image);

        judulEvent.text = e.title;
        deskripsiEvent.text = e.description;

        if (!string.IsNullOrEmpty(e.image))
        {
            StopAllCoroutines(); // ⬅️ DITAMBAHKAN
            StartCoroutine(LoadImage(e.image));
        }
    }

    void OnError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

    IEnumerator LoadImage(string url)
    {
        Debug.Log("Load image from: " + url);

        UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Request error: " + req.error);
            yield break;
        }

        Texture2D tex = DownloadHandlerTexture.GetContent(req);

        if (tex == null)
        {
            Debug.LogError("Texture null - URL bukan direct image");
            yield break;
        }

        // ⬇️ INI INTI PERUBAHANNYA (RAWIMAGE)
        imageEvent.texture = tex;

        // ⬇️ OPSIONAL TAPI DISARANKAN
        imageEvent.color = Color.white;

        Debug.Log("Image loaded SUCCESS");
    }
}
