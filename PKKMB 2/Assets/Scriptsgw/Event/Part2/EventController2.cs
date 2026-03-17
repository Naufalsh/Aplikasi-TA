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
    public string location;
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

    private readonly List<GameObject> generatedPanels = new List<GameObject>();

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

        if (wrapper == null || wrapper.events == null || wrapper.events.Length == 0)
        {
            Debug.LogWarning("Data event kosong");
            return;
        }

        // 🔹 Bersihkan panel lama
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        generatedPanels.Clear();

        // 🔹 Generate panel baru (SEMUA AKTIF)
        foreach (EventDateu eventData in wrapper.events)
        {
            GameObject panel = Instantiate(eventPanelPrefab, contentParent);

            // 🔥 PAKSA PANEL AKTIF
            panel.SetActive(true);

            EventUI2 panelUI = panel.GetComponent<EventUI2>();
            if (panelUI == null)
            {
                Debug.LogError("EventUI2 tidak ditemukan di prefab EventPanel");
                continue;
            }

            panelUI.SetData(eventData);

            generatedPanels.Add(panel);
        }

        // 🔹 Pastikan SEMUA panel tetap aktif (anti carousel off)
        foreach (GameObject panel in generatedPanels)
        {
            panel.SetActive(true);
        }

        // 🔹 Kirim ke carousel
        if (carouselManager != null)
        {
            carouselManager.SetContents(generatedPanels);
        }
        else
        {
            Debug.LogWarning("CarouselManager belum di-assign");
        }
    }

    void OnGetTitleDataError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
    }
}
