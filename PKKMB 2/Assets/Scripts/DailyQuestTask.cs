using UnityEngine;
using TMPro; // Wajib untuk TextMeshPro
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class DailyQuestTask : MonoBehaviour
{
    [Header("Main References")]
    // Drag objek "DailyQuest" (parent dari Quest1 & Quest2) ke sini
    public GameObject contentPanel;

    [Header("--- KONFIGURASI QUEST 1 ---")]
    public GameObject quest1Obj;
    public TextMeshProUGUI titleQuest1;
    public TextMeshProUGUI descQuest1;
    public Button startButton1; // <-- TAMBAHAN BARU: Variabel untuk tombol Start Quest 1

    [Header("--- KONFIGURASI QUEST 2 ---")]
    public GameObject quest2Obj;
    public TextMeshProUGUI titleQuest2;
    public TextMeshProUGUI descQuest2;
    public Button startButton2; // <-- TAMBAHAN BARU: Variabel untuk tombol Start Quest 2

    [Header("Data Keys")]
    private const string KEY_MASTER_DAILY = "DailyQuest";
    private const string KEY_CURRENT_QUESTS = "CurrentDaily";
    private const string KEY_LAST_DATE = "LastQuestDate";

    private Action onCompleteCallback;

    public void LoadDailyQuest(Action onComplete)
    {
        this.onCompleteCallback = onComplete;
        if (contentPanel != null) contentPanel.SetActive(true);

        PlayFabClientAPI.GetUserData(new GetUserDataRequest
        {
            Keys = new List<string> { KEY_CURRENT_QUESTS, KEY_LAST_DATE }
        }, OnUserDataReceived, OnError);
    }

    void OnUserDataReceived(GetUserDataResult userResult)
    {
        string lastDate = userResult.Data.ContainsKey(KEY_LAST_DATE) ? userResult.Data[KEY_LAST_DATE].Value : "";
        string todayDate = DateTime.Now.ToString("yyyy-MM-dd");

        // Skenario 1: Hari Masih Sama (Load dari Cache)
        if (lastDate == todayDate && userResult.Data.ContainsKey(KEY_CURRENT_QUESTS))
        {
            Debug.Log("📅 Hari sama. Load data tersimpan.");
            string jsonCurrent = userResult.Data[KEY_CURRENT_QUESTS].Value;
            WrapperDaily currentWrapper = JsonUtility.FromJson<WrapperDaily>(jsonCurrent);
            DisplayQuests(currentWrapper.list);
            onCompleteCallback?.Invoke();
        }
        // Skenario 2: Hari Baru (Ambil Master Data & Acak)
        else
        {
            Debug.Log("📅 Hari baru! Generate quest baru...");
            PlayFabClientAPI.GetTitleData(new GetTitleDataRequest { Keys = new List<string> { KEY_MASTER_DAILY } },
                titleResult => GenerateNewDailyQuests(titleResult, todayDate),
                OnError);
        }
    }

    void GenerateNewDailyQuests(GetTitleDataResult titleResult, string todayDate)
    {
        if (!titleResult.Data.ContainsKey(KEY_MASTER_DAILY))
        {
            Debug.LogError("Master DailyQuest tidak ditemukan!");
            onCompleteCallback?.Invoke();
            return;
        }

        string rawJson = titleResult.Data[KEY_MASTER_DAILY];
        // Trik parsing JSON Array
        string wrappedJson = "{\"list\":" + rawJson + "}";
        WrapperDaily masterWrapper = JsonUtility.FromJson<WrapperDaily>(wrappedJson);
        List<SimpleBuildingData> masterList = masterWrapper.list;

        List<SimpleBuildingData> pickedQuests = new List<SimpleBuildingData>();
        List<SimpleBuildingData> pool = new List<SimpleBuildingData>(masterList);

        // Ambil 2 quest acak
        for (int i = 0; i < 2; i++)
        {
            if (pool.Count == 0) break;
            int rnd = UnityEngine.Random.Range(0, pool.Count);
            pickedQuests.Add(pool[rnd]);
            pool.RemoveAt(rnd);
        }

        SaveDailyProgress(pickedQuests, todayDate);
        DisplayQuests(pickedQuests);
    }

    void SaveDailyProgress(List<SimpleBuildingData> currentQuests, string date)
    {
        WrapperDaily wrapper = new WrapperDaily { list = currentQuests };
        string jsonCurrent = JsonUtility.ToJson(wrapper);

        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { KEY_CURRENT_QUESTS, jsonCurrent },
                { KEY_LAST_DATE, date }
            }
        },
        result =>
        {
            Debug.Log("✅ Daily Quest tersimpan!");
            onCompleteCallback?.Invoke();
        },
        error =>
        {
            Debug.LogError("Gagal simpan user data");
            onCompleteCallback?.Invoke();
        });
    }

    // --- FUNGSI TAMPILKAN KE UI ---
    void DisplayQuests(List<SimpleBuildingData> quests)
    {
        // ---------------- SETUP QUEST 1 ----------------
        if (quests.Count > 0)
        {
            if (quest1Obj != null) quest1Obj.SetActive(true);
            if (titleQuest1 != null) titleQuest1.text = quests[0].name;
            if (descQuest1 != null) descQuest1.text = $"Find the {quests[0].name} to unlock your next quest!";

            // Perbaikan: Gunakan startButton1, bukan questSlot1
            if (startButton1 != null)
            {
                startButton1.onClick.RemoveAllListeners();
                string idTujuan = quests[0].id; // Capture ID

                startButton1.onClick.AddListener(() =>
                {
                    if (RouteManager.Instance != null)
                    {
                        RouteManager.Instance.ClearRoute();
                        RouteManager.Instance.DrawRouteToBuilding(idTujuan);
                    }
                });
            }
        }
        else
        {
            if (quest1Obj != null) quest1Obj.SetActive(false);
        }

        // ---------------- SETUP QUEST 2 ----------------
        if (quests.Count > 1)
        {
            if (quest2Obj != null) quest2Obj.SetActive(true);
            if (titleQuest2 != null) titleQuest2.text = quests[1].name;
            if (descQuest2 != null) descQuest2.text = $"Find the {quests[1].name} to unlock your next quest!";

            // Perbaikan: Gunakan startButton2, bukan questSlot2
            if (startButton2 != null)
            {
                startButton2.onClick.RemoveAllListeners();
                string idTujuan = quests[1].id;

                startButton2.onClick.AddListener(() =>
                {
                    if (RouteManager.Instance != null)
                    {
                        RouteManager.Instance.ClearRoute();
                        RouteManager.Instance.DrawRouteToBuilding(idTujuan);
                    }
                });
            }
        }
        else
        {
            if (quest2Obj != null) quest2Obj.SetActive(false);
        }
    }

    void OnError(PlayFabError error)
    {
        Debug.LogError("Error DailyQuest: " + error.GenerateErrorReport());
        onCompleteCallback?.Invoke();
    }
}

// Wrapper khusus file ini
[System.Serializable]
public class WrapperDaily
{
    public List<SimpleBuildingData> list;
}

// Pastikan SimpleBuildingData SUDAH ADA di script lain (misal SpecialEventTask.cs).
// Jika belum ada / error not found, hilangkan tanda komentar (//) di bawah ini:

/*
[System.Serializable]
public class SimpleBuildingData
{
    public string id;
    public string name;
}
*/