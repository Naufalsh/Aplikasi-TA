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
    public Image imageEvent;

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
        if (!result.Data.ContainsKey("Events")) return;

        string wrapped = "{\"events\":" + result.Data["Events"] + "}";
        EventWrapper wrapper = JsonUtility.FromJson<EventWrapper>(wrapped);

        if (wrapper.events.Length == 0) return;

        EventData e = wrapper.events[0];
        judulEvent.text = e.title;
        deskripsiEvent.text = e.description;

        if (!string.IsNullOrEmpty(e.image))
            StartCoroutine(LoadImage(e.image));
    }

    void OnError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }

    IEnumerator LoadImage(string url)
    {
        UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = ((DownloadHandlerTexture)req.downloadHandler).texture;
            imageEvent.sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );
        }
    }
}
