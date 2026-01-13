using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Text;
using System;

public class SpecialEventTask : MonoBehaviour
{
    [Header("UI References")]
    public GameObject contentPanel;
    public TextMeshProUGUI descriptionText;

    [Header("Button Config")]
    public Button actionButton;
    public TextMeshProUGUI buttonText;

    [Header("Data Keys")]
    private string keySpecialEvent = "SpecialEvent";
    private string keyBuildingLocation = "BuildingLocation";
    private string keyUserDataUnlock = "unlockBuilding";

    // Cache Data
    private Dictionary<string, string> buildingNameCache = new Dictionary<string, string>();
    private List<string> eventBuildingIds = new List<string>();
    private HashSet<string> unlockedBuildingIds = new HashSet<string>();

    // --- FUNGSI UTAMA: LOAD DATA ---
    public void LoadSpecialEventData(Action onComplete)
    {
        if (contentPanel != null) contentPanel.SetActive(true);

        // Reset tombol ke state loading/default dulu agar user tidak asal klik
        if (actionButton != null) actionButton.interactable = false;

        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest
        {
            Keys = new List<string> { keySpecialEvent, keyBuildingLocation }
        },
        titleResult =>
        {
            PlayFabClientAPI.GetUserData(new GetUserDataRequest
            {
                Keys = new List<string> { keyUserDataUnlock }
            },
            userResult =>
            {
                ProcessAllData(titleResult, userResult);
                onComplete?.Invoke();
            },
            error =>
            {
                OnError(error);
                onComplete?.Invoke();
            });
        },
        error =>
        {
            OnError(error);
            onComplete?.Invoke();
        });
    }

    void ProcessAllData(GetTitleDataResult titleResult, GetUserDataResult userResult)
    {
        unlockedBuildingIds.Clear();
        if (userResult.Data != null && userResult.Data.ContainsKey(keyUserDataUnlock))
        {
            string csv = userResult.Data[keyUserDataUnlock].Value;
            string[] ids = csv.Split(',');
            foreach (string id in ids) unlockedBuildingIds.Add(id);
        }

        if (!titleResult.Data.ContainsKey(keySpecialEvent) || !titleResult.Data.ContainsKey(keyBuildingLocation)) return;

        ProcessBuildingData(titleResult.Data[keyBuildingLocation]);
        ProcessEventData(titleResult.Data[keySpecialEvent]);
    }

    void ProcessBuildingData(string json)
    {
        BuildingLocationWrapper wrapper = JsonUtility.FromJson<BuildingLocationWrapper>(json);
        buildingNameCache.Clear();
        foreach (var b in wrapper.buildings)
        {
            if (!buildingNameCache.ContainsKey(b.id)) buildingNameCache.Add(b.id, b.name);
        }
    }

    void ProcessEventData(string json)
    {
        SpecialEventWrapper eventData = JsonUtility.FromJson<SpecialEventWrapper>(json);
        eventBuildingIds.Clear();

        // 1. Cek Status Kelengkapan
        bool allCompleted = true;
        foreach (var q in eventData.quests)
        {
            eventBuildingIds.Add(q.buildingId);
            if (!unlockedBuildingIds.Contains(q.buildingId))
            {
                allCompleted = false;
            }
        }

        // 2. Atur Tampilan Berdasarkan Status
        if (allCompleted)
        {
            // --- KONDISI: SUDAH SELESAI SEMUA ---
            if (descriptionText != null)
            {
                // Kata-kata yang lebih baik untuk deskripsi selesai
                descriptionText.text = "Event Completed\n\nYou have successfully visited all required locations.";
                descriptionText.alignment = TextAlignmentOptions.Center;
            }

            if (actionButton != null && buttonText != null)
            {
                buttonText.text = "Done";
                actionButton.interactable = false; // Tombol mati total
                actionButton.onClick.RemoveAllListeners();
            }
        }
        else
        {
            // --- KONDISI: BELUM SELESAI ---
            if (descriptionText != null) descriptionText.alignment = TextAlignmentOptions.TopLeft;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("When you press start, you must follow the blue directions and complete the quests in each building.");
            sb.AppendLine("");
            sb.AppendLine("Buildings that must be visited :");
            sb.AppendLine("");

            int number = 1;
            foreach (var q in eventData.quests)
            {
                string bName = "Unknown";
                if (buildingNameCache.ContainsKey(q.buildingId)) bName = buildingNameCache[q.buildingId];
                else bName = q.Destination;

                bool isItemDone = unlockedBuildingIds.Contains(q.buildingId);
                if (isItemDone)
                {
                    sb.AppendLine($"{number}. <s>{bName}</s> <color=green>Done</color>");
                }
                else
                {
                    sb.AppendLine($"{number}. {bName}");
                }
                number++;
            }

            if (descriptionText != null) descriptionText.text = sb.ToString();

            // Set Tombol menjadi "Start" dan Interactable
            if (actionButton != null && buttonText != null)
            {
                buttonText.text = "Start";
                actionButton.interactable = true;

                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(OnStartButtonClicked);
            }
        }
    }

    // --- LOGIKA SAAT TOMBOL DITEKAN ---
    void OnStartButtonClicked()
    {
        if (eventBuildingIds.Count > 0 && RouteManager.Instance != null)
        {
            Debug.Log("🚀 Memulai Special Event Tour!");

            // 1. Jalankan Navigasi
            RouteManager.Instance.ClearRoute();
            RouteManager.Instance.StartCampusTour(eventBuildingIds);

            // 2. Ubah UI Button Seketika
            if (buttonText != null) buttonText.text = "In Progress"; // Bahasa Inggris yang lebih natural
            if (actionButton != null) actionButton.interactable = false; // Matikan tombol agar tidak bisa diklik lagi
        }
        else
        {
            Debug.LogError("Gagal memulai navigasi.");
        }
    }

    void OnError(PlayFabError error)
    {
        Debug.LogError("Error: " + error.GenerateErrorReport());
    }
}

// Wrapper Class tetap sama
[System.Serializable]
public class SpecialEventWrapper { public int reward; public List<SpecialQuestItem> quests; }
[System.Serializable]
public class SpecialQuestItem { public string QuestId; public string buildingId; public string Destination; }
[System.Serializable]
public class BuildingLocationWrapper { public List<SimpleBuildingData> buildings; }
[System.Serializable]
public class SimpleBuildingData { public string id; public string name; }