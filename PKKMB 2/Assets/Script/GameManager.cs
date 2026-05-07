using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;

[System.Serializable]
public class BuildingData
{
    public string id;
    public string name;
    public string description;
    public string url;
}

[System.Serializable]
public class BuildingDataWrapper
{
    public List<BuildingData> buildings;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Dictionary<string, BuildingData> buildingCache = new();
    public GameObject targetPanel; // Drag panel ke sini lewat Inspector
    public float displayTime = 5f;

    //Event
    public string eventsRawJson;
    public bool eventsLoaded = false;
    public string currentEventBuildingId;
    public EventDataQuiz currentSelectedEvent;
    public List<EventDataQuiz> allEvents = new();
    //
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadBuildingData();
        LoadEvents();
    }

    public void RefreshScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void LoadBuildingData()
    {
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(),
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey("BuildingLocation"))
                {
                    string rawJson = result.Data["BuildingLocation"];
                    try
                    {
                        var wrapper = JsonUtility.FromJson<BuildingDataWrapper>(rawJson);
                        foreach (var building in wrapper.buildings)
                        {
                            if (!buildingCache.ContainsKey(building.id))
                            {
                                buildingCache.Add(building.id, building);
                            }
                        }
                        Debug.Log("total buildingCache: " + buildingCache.Count);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Gagal parse JSON: " + e.Message);
                    }
                }
                else
                {
                    Debug.Log("Tidak ada data gedung di PlayFab");
                }
            },
            error => Debug.LogError("Gagal ambil data: " + error.GenerateErrorReport()));
    }

    private void OnEnable()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(true); // Tampilkan panel
            Invoke("HidePanel", displayTime); // Panggil HidePanel setelah 5 detik
        }
    }

    private void HidePanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false); // Sembunyikan panel
        }
    }

    //CHECK EVENT
    // public void LoadEvents()
    // {
    //     PlayFabClientAPI.GetTitleData(
    //         new GetTitleDataRequest(),
    //         result =>
    //         {
    //             if (result.Data.ContainsKey("Events"))
    //             {
    //                 eventsRawJson = result.Data["Events"];
    //                 eventsLoaded = true;
    //                 Debug.Log("Events loaded");
    //                 Debug.Log("RAW EVENTS: " + eventsRawJson);
    //             }
    //         },
    //         error =>
    //         {
    //             Debug.LogError(error.GenerateErrorReport());
    //         });
    // }

    public void LoadEvents()
{
    PlayFabClientAPI.GetTitleData(
        new GetTitleDataRequest(),
        result =>
        {
            if (result.Data.ContainsKey("Events"))
            {
                eventsRawJson = result.Data["Events"];

                string wrappedJson = "{ \"events\": " + eventsRawJson + "}";

                EventListWrapper wrapper =
                    JsonUtility.FromJson<EventListWrapper>(wrappedJson);

                allEvents = wrapper.events;

                eventsLoaded = true;

                Debug.Log("Events loaded: " + allEvents.Count);
            }
        },
        error =>
        {
            Debug.LogError(error.GenerateErrorReport());
        });
}

    public bool HasEventAtBuilding(string buildingId)
    {
        if (!eventsLoaded) return false;

        string normalizedJson = eventsRawJson.Replace(" ", "");

        return normalizedJson.Contains($"\"location\":\"{buildingId}\"");
    }

    //LOAD EVENT QUIZ
    public EventDataQuiz GetEventByLocation(string buildingId)
    {
        string wrappedJson = "{ \"events\": " + eventsRawJson + "}";

        EventListWrapper wrapper =
            JsonUtility.FromJson<EventListWrapper>(wrappedJson);

        foreach (var ev in wrapper.events)
        {
            if (ev.location == buildingId)
                return ev;
        }

        return null;
    }
}