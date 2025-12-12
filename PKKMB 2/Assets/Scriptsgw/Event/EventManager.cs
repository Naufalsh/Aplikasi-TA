using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.Networking;

// Class untuk menampung data event
[System.Serializable]
public class EventData
{
    public string title;
    public string description;
    public string image;
}

// Wrapper supaya JsonUtility bisa parsing array
[System.Serializable]
public class EventWrapper
{
    public EventData[] events;
}

public class EventManager : MonoBehaviour
{
    public TextMeshProUGUI judulEvent;
    public TextMeshProUGUI deskripsiEvent;
    public Image imageEvent;

    void Start()
    {
        GetEventFromPlayFab();
    }

    void GetEventFromPlayFab()
    {
        // Memanggil TitleData dari PlayFab
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), OnGetTitleDataSuccess, OnGetTitleDataError);
    }

    void OnGetTitleDataSuccess(GetTitleDataResult result)
    {
        if (result.Data != null && result.Data.ContainsKey("Events"))
        {
            string eventJson = result.Data["Events"];

            // Bungkus JSON array menjadi object supaya JsonUtility bisa parse
            string wrappedJson = "{\"events\":" + eventJson + "}";

            EventWrapper wrapper = JsonUtility.FromJson<EventWrapper>(wrappedJson);

            if (wrapper.events.Length > 0)
            {
                EventData eventData = wrapper.events[0]; // ambil event pertama saja

                // Update UI
                judulEvent.text = eventData.title;
                deskripsiEvent.text = eventData.description;

                if (!string.IsNullOrEmpty(eventData.image))
                {
                    StartCoroutine(LoadImage(eventData.image));
                }
            }
            else
            {
                Debug.LogWarning("Tidak ada event di dalam array!");
            }
        }
        else
        {
            Debug.LogWarning("Data Event tidak ditemukan di TitleData!");
        }
    }

    void OnGetTitleDataError(PlayFabError error)
    {
        Debug.LogError("Gagal mengambil TitleData: " + error.GenerateErrorReport());
    }

    IEnumerator LoadImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Gagal load image: " + request.error);
        }
        else
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            imageEvent.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
