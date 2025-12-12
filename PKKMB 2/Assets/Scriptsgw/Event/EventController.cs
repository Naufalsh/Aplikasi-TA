using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.Networking;

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

public class EventController : MonoBehaviour
{
    public Transform contentParent; // Content dari ScrollView
    public GameObject eventPanelPrefab; // Prefab EventPanel

    void Start()
    {
        GetEventFromPlayFab();
    }

    void GetEventFromPlayFab()
    {
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), OnGetTitleDataSuccess, OnGetTitleDataError);
    }

    void OnGetTitleDataSuccess(GetTitleDataResult result)
    {
        if (result.Data != null && result.Data.ContainsKey("Events"))
        {
            string eventJson = result.Data["Events"];
            string wrappedJson = "{\"events\":" + eventJson + "}";
            EventWrapper2 wrapper = JsonUtility.FromJson<EventWrapper2>(wrappedJson);

            foreach (var eventData in wrapper.events)
            {
                GameObject panel = Instantiate(eventPanelPrefab, contentParent);
                EventPanelUI panelUI = panel.GetComponent<EventPanelUI>();
                panelUI.SetData(eventData);
            }
        }
    }

    void OnGetTitleDataError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }
}
