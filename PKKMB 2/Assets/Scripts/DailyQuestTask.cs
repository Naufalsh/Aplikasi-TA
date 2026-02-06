using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DailyQuestTask : MonoBehaviour
{
    public static DailyQuestTask Instance;

    [Header("Main References")]
    public GameObject contentPanel;

    [Header("--- KONFIGURASI QUEST 1 ---")]
    public GameObject quest1Obj;
    public TextMeshProUGUI titleQuest1;
    public TextMeshProUGUI descQuest1;
    public Button startButton1;

    [Header("--- KONFIGURASI QUEST 2 ---")]
    public GameObject quest2Obj;
    public TextMeshProUGUI titleQuest2;
    public TextMeshProUGUI descQuest2;
    public Button startButton2;

    [Header("Button Labels")]
    public string labelStart = "Start";
    public string labelNavigating = "Navigating";
    public string labelDone = "Done";

    [Header("Data Keys")]
    private const string KEY_MASTER_DAILY   = "DailyQuest";      // TitleData
    private const string KEY_CURRENT_QUESTS = "CurrentDaily";    // UserData
    private const string KEY_LAST_DATE      = "LastQuestDate";   // UserData (WIB date key from PlayFab time -> +7)
    private const string KEY_DAILY_CHECKINS = "DailyCheckins";   // UserData: {"checkedIds":[...]}

    private Action onCompleteCallback;

    // Cache
    private List<SimpleBuildingData> cachedTargets = new List<SimpleBuildingData>();
    private HashSet<string> checkedToday = new HashSet<string>();

    // IMPORTANT: ini hanya LOCAL (tidak disimpan ke PlayFab)
    // Jadi kalau pindah scene, status Navigating hilang -> tombol balik Start (seperti PlayerTask)
    private string activeTargetId = "";

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        // Saat pindah scene, samakan behavior seperti PlayerTask:
        // route hilang -> status navigating juga reset -> tombol balik Start
        ClearLocalNavigationState("Scene changed");
    }

    // ======================================================
    // ENTRY
    // ======================================================
    public void LoadDailyQuest(Action onComplete)
    {
        this.onCompleteCallback = onComplete;
        if (contentPanel != null) contentPanel.SetActive(true);

        // Ambil waktu server, konversi ke WIB date (reset 00:00 WIB)
        PlayFabClientAPI.GetTime(new GetTimeRequest(),
            timeResult =>
            {
                DateTime utc = timeResult.Time;
                if (utc.Kind == DateTimeKind.Unspecified)
                    utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

                string todayWibDate = GetWibDateKey(utc);

                PlayFabClientAPI.GetUserData(new GetUserDataRequest
                {
                    Keys = new List<string> {
                        KEY_CURRENT_QUESTS,
                        KEY_LAST_DATE,
                        KEY_DAILY_CHECKINS
                    }
                },
                userResult => OnUserDataReceived(userResult, todayWibDate),
                OnError);
            },
            OnError
        );
    }

    private void OnUserDataReceived(GetUserDataResult userResult, string todayDateWib)
    {
        string lastDate = (userResult.Data != null && userResult.Data.ContainsKey(KEY_LAST_DATE))
            ? (userResult.Data[KEY_LAST_DATE].Value ?? "").Trim()
            : "";

        bool hasCurrent = (userResult.Data != null && userResult.Data.ContainsKey(KEY_CURRENT_QUESTS));

        // Scene reload / buka panel ulang -> reset navigating lokal biar tidak nyangkut
        // (seperti PlayerTask, tidak pernah persist navigating)
        activeTargetId = "";

        if (lastDate == todayDateWib && hasCurrent)
        {
            // Load target harian
            string jsonCurrent = userResult.Data[KEY_CURRENT_QUESTS].Value;
            WrapperDaily currentWrapper = JsonUtility.FromJson<WrapperDaily>(jsonCurrent);
            cachedTargets = (currentWrapper != null && currentWrapper.list != null)
                ? currentWrapper.list
                : new List<SimpleBuildingData>();

            // Load check-in harian
            checkedToday.Clear();
            if (userResult.Data.ContainsKey(KEY_DAILY_CHECKINS))
            {
                DailyCheckinWrapper w = JsonUtility.FromJson<DailyCheckinWrapper>(userResult.Data[KEY_DAILY_CHECKINS].Value);
                if (w != null && w.checkedIds != null)
                    foreach (var id in w.checkedIds) checkedToday.Add(id);
            }

            DisplayQuests(cachedTargets);
            RefreshDailyButtons();
            onCompleteCallback?.Invoke();
        }
        else
        {
            // Hari baru: generate
            PlayFabClientAPI.GetTitleData(
                new GetTitleDataRequest { Keys = new List<string> { KEY_MASTER_DAILY } },
                titleResult => GenerateNewDailyQuests(titleResult, todayDateWib),
                OnError
            );
        }
    }

    // ======================================================
    // GENERATE DAILY
    // ======================================================
    private void GenerateNewDailyQuests(GetTitleDataResult titleResult, string todayDateWib)
    {
        if (titleResult.Data == null || !titleResult.Data.ContainsKey(KEY_MASTER_DAILY))
        {
            Debug.LogError("Master DailyQuest tidak ditemukan!");
            onCompleteCallback?.Invoke();
            return;
        }

        string rawJson = titleResult.Data[KEY_MASTER_DAILY];
        string wrappedJson = "{\"list\":" + rawJson + "}";
        WrapperDaily masterWrapper = JsonUtility.FromJson<WrapperDaily>(wrappedJson);

        List<SimpleBuildingData> masterList = (masterWrapper != null && masterWrapper.list != null)
            ? masterWrapper.list
            : new List<SimpleBuildingData>();

        if (masterList.Count == 0)
        {
            Debug.LogError("Master list DailyQuest kosong!");
            onCompleteCallback?.Invoke();
            return;
        }

        List<SimpleBuildingData> pickedQuests = new List<SimpleBuildingData>();
        List<SimpleBuildingData> pool = new List<SimpleBuildingData>(masterList);

        for (int i = 0; i < 2; i++)
        {
            if (pool.Count == 0) break;
            int rnd = UnityEngine.Random.Range(0, pool.Count);
            pickedQuests.Add(pool[rnd]);
            pool.RemoveAt(rnd);
        }

        cachedTargets = pickedQuests;
        checkedToday.Clear();
        activeTargetId = ""; // reset navigating lokal

        SaveDailyProgress(pickedQuests, todayDateWib);

        DisplayQuests(pickedQuests);
        RefreshDailyButtons();
    }

    private void SaveDailyProgress(List<SimpleBuildingData> currentQuests, string dateWib)
    {
        WrapperDaily wrapper = new WrapperDaily { list = currentQuests };
        string jsonCurrent = JsonUtility.ToJson(wrapper);

        DailyCheckinWrapper checkin = new DailyCheckinWrapper { checkedIds = new List<string>() };
        string jsonCheckin = JsonUtility.ToJson(checkin);

        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { KEY_CURRENT_QUESTS, jsonCurrent },
                { KEY_LAST_DATE, dateWib },
                { KEY_DAILY_CHECKINS, jsonCheckin }
            }
        },
        _ => { onCompleteCallback?.Invoke(); },
        error =>
        {
            Debug.LogError("Gagal simpan user data: " + error.GenerateErrorReport());
            onCompleteCallback?.Invoke();
        });
    }

    // ======================================================
    // FLOW: klik Start -> Navigating (LOCAL ONLY)
    // ======================================================
    private void StartNavigationTo(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId)) return;

        // kalau sudah done, jangan mulai lagi
        if (checkedToday.Contains(buildingId)) return;

        // set target aktif (lokal)
        activeTargetId = buildingId;
        RefreshDailyButtons();

        // gambar route kalau RouteManager ada (Gameplay)
        if (RouteManager.Instance != null)
        {
            RouteManager.Instance.ClearRoute();
            RouteManager.Instance.DrawRouteToBuilding(buildingId);
        }
        else
        {
            // kalau bukan scene gameplay, jangan bikin stuck
            ClearLocalNavigationState("RouteManager not found");
        }
    }

    private void ClearLocalNavigationState(string reason)
    {
        if (!string.IsNullOrEmpty(activeTargetId))
            Debug.Log($"[DailyQuest] Clear navigation state. Reason: {reason}");

        activeTargetId = "";

        // Tidak perlu update PlayFab karena ini state lokal
        RefreshDailyButtons();
    }

    // ======================================================
    // AUTO CHECK-IN: masuk collider -> Done
    // (tetap mengikuti flow kamu: harus start dulu baru bisa done)
    // ======================================================
    public void TryAutoCheckin(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId)) return;
        if (cachedTargets == null || cachedTargets.Count == 0) return;

        bool isTarget = cachedTargets.Exists(q => q.id == buildingId);
        if (!isTarget) return;

        // harus sedang dinavigasi
        if (string.IsNullOrEmpty(activeTargetId) || buildingId != activeTargetId) return;

        if (checkedToday.Contains(buildingId)) return;

        checkedToday.Add(buildingId);

        // selesai -> reset state navigating + clear route
        activeTargetId = "";
        if (RouteManager.Instance != null)
            RouteManager.Instance.ClearRoute();

        SaveCheckinsOnly();
        RefreshDailyButtons();

        Debug.Log($"✅ Done: {buildingId} ({checkedToday.Count}/2)");
    }

    private void SaveCheckinsOnly()
    {
        DailyCheckinWrapper wrapper = new DailyCheckinWrapper
        {
            checkedIds = new List<string>(checkedToday)
        };

        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { KEY_DAILY_CHECKINS, JsonUtility.ToJson(wrapper) }
            }
        },
        _ => Debug.Log("✅ DailyCheckins tersimpan!"),
        error => Debug.LogError("Gagal simpan DailyCheckins: " + error.GenerateErrorReport()));
    }

    // ======================================================
    // UI
    // ======================================================
    private void DisplayQuests(List<SimpleBuildingData> quests)
    {
        // QUEST 1
        if (quests != null && quests.Count > 0)
        {
            if (quest1Obj != null) quest1Obj.SetActive(true);
            if (titleQuest1 != null) titleQuest1.text = quests[0].name;
            if (descQuest1 != null) descQuest1.text = $"Navigate to {quests[0].name} today.";

            if (startButton1 != null)
            {
                startButton1.onClick.RemoveAllListeners();
                string idTujuan = quests[0].id;
                startButton1.onClick.AddListener(() => StartNavigationTo(idTujuan));
            }
        }
        else
        {
            if (quest1Obj != null) quest1Obj.SetActive(false);
        }

        // QUEST 2
        if (quests != null && quests.Count > 1)
        {
            if (quest2Obj != null) quest2Obj.SetActive(true);
            if (titleQuest2 != null) titleQuest2.text = quests[1].name;
            if (descQuest2 != null) descQuest2.text = $"Navigate to {quests[1].name} today.";

            if (startButton2 != null)
            {
                startButton2.onClick.RemoveAllListeners();
                string idTujuan = quests[1].id;
                startButton2.onClick.AddListener(() => StartNavigationTo(idTujuan));
            }
        }
        else
        {
            if (quest2Obj != null) quest2Obj.SetActive(false);
        }
    }

    private void RefreshDailyButtons()
    {
        if (cachedTargets != null && cachedTargets.Count > 0 && startButton1 != null)
            ApplyButtonState(startButton1, cachedTargets[0].id);

        if (cachedTargets != null && cachedTargets.Count > 1 && startButton2 != null)
            ApplyButtonState(startButton2, cachedTargets[1].id);
    }

    private void ApplyButtonState(Button btn, string buildingId)
    {
        bool done = checkedToday.Contains(buildingId);
        bool navigatingThis = (!done && !string.IsNullOrEmpty(activeTargetId) && activeTargetId == buildingId);

        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            if (done) tmp.text = labelDone;
            else if (navigatingThis) tmp.text = labelNavigating;
            else tmp.text = labelStart;
        }

        // sama seperti flow awal kamu:
        // - done: disable
        // - navigating: disable (anti spam)
        // - start: enable
        btn.interactable = (!done && !navigatingThis);
    }

    private string GetWibDateKey(DateTime utcTime)
    {
        DateTime wib = utcTime.AddHours(7);
        return wib.ToString("yyyy-MM-dd");
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError("Error DailyQuest: " + error.GenerateErrorReport());
        onCompleteCallback?.Invoke();
    }
}

// Wrapper
[System.Serializable]
public class WrapperDaily
{
    public List<SimpleBuildingData> list;
}

[System.Serializable]
public class DailyCheckinWrapper
{
    public List<string> checkedIds = new List<string>();
}
