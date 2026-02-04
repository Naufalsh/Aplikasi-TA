using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

[System.Serializable]
public class EventDateu
{
    public string title;
    public string description;
    public string image;
}

[System.Serializable]
public class EventWrapper2
{
    public EventDateu[] events;
}

public class EventController2 : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentParent;
    public GameObject eventPanelPrefab;
    public CarauselManager carouselManager;

    private List<GameObject> generatedPanels = new List<GameObject>();

    void Start()
    {
        GetEventFromPlayFab();
    }

    void GetEventFromPlayFab()
    {
        PlayFabClientAPI.GetTitleData(
            new GetTitleDataRequest(),
            OnGetTitleDataSuccess,
            OnGetTitleDataError
        );
    }

    void OnGetTitleDataSuccess(GetTitleDataResult result)
    {
        if (result.Data == null || !result.Data.ContainsKey("Events"))
        {
            Debug.LogWarning("TitleData 'Events' tidak ditemukan");
            return;
        }

        string eventJson = result.Data["Events"];
        string wrappedJson = "{\"events\":" + eventJson + "}";
        EventWrapper2 wrapper = JsonUtility.FromJson<EventWrapper2>(wrappedJson);

        if (wrapper.events == null || wrapper.events.Length == 0)
        {
            Debug.LogWarning("Data event kosong");
            return;
        }

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        generatedPanels.Clear();

        foreach (var eventData in wrapper.events)
        {
            GameObject panel = Instantiate(eventPanelPrefab, contentParent);
            panel.SetActive(true);

            EventUI2 panelUI = panel.GetComponent<EventUI2>();
            panelUI.SetData(eventData);

            panel.SetActive(false);

            generatedPanels.Add(panel);
        }

        carouselManager.SetContents(generatedPanels);
    }

    void OnGetTitleDataError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }
}
