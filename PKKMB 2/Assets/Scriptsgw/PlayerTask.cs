using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;

[Serializable]
public class Questaw
{
    public string QuestId;
    public string Destination;
    public string buildingId;
}

[Serializable]
public class QuestListWrapper
{
    public List<Questaw> quests;
}

public class PlayerTask : MonoBehaviour
{
    [Header("Quest UI Title")]
    public TextMeshProUGUI title1;
    public TextMeshProUGUI title2;
    public TextMeshProUGUI title3;

    [Header("Quest UI Description")]
    public TextMeshProUGUI description1;
    public TextMeshProUGUI description2;
    public TextMeshProUGUI description3;

    [Header("Quest Start Buttons (GameObject yang ada Button + Text)")]
    public GameObject start1;
    public GameObject start2;
    public GameObject start3;

    [Header("Done Objects")]
    public GameObject done1;
    public GameObject done2;
    public GameObject done3;

    [Header("Loading UI")]
    public GameObject mamah;
    public GameObject loading;

    [Header("Button Labels")]
    public string labelStart = "Start";
    public string labelNavigating = "Navigating";

    // Data
    private List<string> unlockedBuildingIds = new List<string>();
    private List<Questaw> currentQuests = new List<Questaw>();

    // State navigasi saat ini (biar tombol punya status)
    private string activeTargetId = "";

    private void Start()
    {
        // Optional: kamu bisa log kalau perlu
    }

    public void LoadPKKMBQuests()
    {
        if (loading != null) loading.SetActive(true);
        if (mamah != null) mamah.SetActive(false);

        Debug.Log("GameModeManager memanggil loading quest PKKMB.");
        StartQuestLoadingProcess();
    }

    // =========================
    // Chain loading
    // =========================
    private void StartQuestLoadingProcess()
    {
        Debug.Log("Langkah 1: Mengambil GroupNumber pemain...");
        PlayFabClientAPI.GetUserData(new GetUserDataRequest { Keys = new List<string> { "GroupNumber" } },
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey("GroupNumber"))
                {
                    string groupStr = (result.Data["GroupNumber"].Value ?? "").Trim();
                    if (int.TryParse(groupStr, out int groupNumber))
                    {
                        GetUnlockBuilding(groupNumber);
                    }
                    else
                    {
                        Debug.LogError("Gagal parse GroupNumber dari UserData: " + groupStr);
                        EndLoadingUI();
                    }
                }
                else
                {
                    Debug.LogWarning("Pemain tidak memiliki 'GroupNumber' di UserData.");
                    EndLoadingUI();
                }
            },
            error => { OnPlayFabError(error); EndLoadingUI(); });
    }

    private void GetUnlockBuilding(int groupNumber)
    {
        Debug.Log("Langkah 2: Mengambil data unlockBuilding...");
        PlayFabClientAPI.GetUserData(new GetUserDataRequest { Keys = new List<string> { "unlockBuilding" } },
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey("unlockBuilding"))
                {
                    string data = result.Data["unlockBuilding"].Value ?? "";
                    unlockedBuildingIds = new List<string>(data.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                    Debug.Log("Unlocked Buildings Loaded: " + string.Join(", ", unlockedBuildingIds));
                }
                else
                {
                    unlockedBuildingIds.Clear();
                    Debug.Log("No unlockBuilding data found.");
                }

                FetchQuestsForGroup(groupNumber);
            },
            error => { OnPlayFabError(error); EndLoadingUI(); });
    }

    private void FetchQuestsForGroup(int groupNumber)
    {
        Debug.Log($"Langkah 3: Meminta quest untuk grup {groupNumber} dari CloudScript...");
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "getQuestsByGroup",
            FunctionParameter = new { groupCode = groupNumber }
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnFetchQuestsSuccess, error => { OnPlayFabError(error); EndLoadingUI(); });
    }

    private void OnFetchQuestsSuccess(ExecuteCloudScriptResult result)
    {
        Debug.Log("Langkah 4: Berhasil menerima data quest dari CloudScript!");
        if (result.FunctionResult != null && !string.IsNullOrEmpty(result.FunctionResult.ToString()))
        {
            try
            {
                var wrapper = JsonUtility.FromJson<QuestListWrapper>(result.FunctionResult.ToString());
                if (wrapper != null && wrapper.quests != null)
                {
                    ApplyQuestToUI(wrapper.quests);
                    return;
                }

                Debug.LogError("Hasil JSON dari CloudScript tidak valid atau tidak berisi 'quests'.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Gagal parse JSON dari CloudScript: {e.Message}. JSON diterima: {result.FunctionResult}");
            }
        }
        else
        {
            Debug.LogWarning("CloudScript tidak mengembalikan data. Mungkin grup tidak ditemukan.");
        }

        EndLoadingUI();
    }

    // =========================
    // UI + status button
    // =========================
    private void ApplyQuestToUI(List<Questaw> quests)
    {
        currentQuests = quests ?? new List<Questaw>();

        // QUEST 1
        if (currentQuests.Count > 0)
        {
            title1.text = currentQuests[0].Destination;
            description1.text = "Find the " + currentQuests[0].Destination + " To complete the quests!";
        }
        else
        {
            title1.text = "-";
            description1.text = "-";
        }

        // QUEST 2
        if (currentQuests.Count > 1)
        {
            title2.text = currentQuests[1].Destination;
            description2.text = "Find the " + currentQuests[1].Destination + " To complete the quests!";
        }
        else
        {
            title2.text = "-";
            description2.text = "-";
        }

        // QUEST 3
        if (currentQuests.Count > 2)
        {
            title3.text = currentQuests[2].Destination;
            description3.text = "Find the " + currentQuests[2].Destination + " To complete the quests!";
        }
        else
        {
            title3.text = "-";
            description3.text = "-";
        }

        RefreshQuestButtons();

        if (mamah != null) mamah.SetActive(true);
        if (loading != null) loading.SetActive(false);
    }

    private void RefreshQuestButtons()
    {
        SetSlotUI(0, start1, done1);
        SetSlotUI(1, start2, done2);
        SetSlotUI(2, start3, done3);
    }

    private void SetSlotUI(int index, GameObject startObj, GameObject doneObj)
    {
        if (startObj == null || doneObj == null) return;

        // kalau quest slot tidak ada datanya
        if (currentQuests == null || index >= currentQuests.Count)
        {
            startObj.SetActive(false);
            doneObj.SetActive(false);
            return;
        }

        string buildingId = currentQuests[index].buildingId;
        bool done = unlockedBuildingIds.Contains(buildingId);
        bool navigatingThis = (!done && !string.IsNullOrEmpty(activeTargetId) && activeTargetId == buildingId);

        // show/hide start vs done
        doneObj.SetActive(done);
        startObj.SetActive(!done);

        if (done) return; // sudah done, tidak perlu set tombol

        // set text + interactable
        Button btn = startObj.GetComponent<Button>();
        TextMeshProUGUI txt = startObj.GetComponentInChildren<TextMeshProUGUI>(true);

        if (txt != null)
            txt.text = navigatingThis ? labelNavigating : labelStart;

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();

            // kalau sedang navigating, tombolnya dimatikan biar tidak spam
            btn.interactable = !navigatingThis;

            // kalau belum navigating, klik untuk mulai navigasi
            if (!navigatingThis)
            {
                btn.onClick.AddListener(() => StartNavigationTo(buildingId));
            }
        }
    }

    private void StartNavigationTo(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId)) return;

        activeTargetId = buildingId;
        RefreshQuestButtons();

        if (RouteManager.Instance == null)
        {
            Debug.LogError("RouteManager.Instance tidak ditemukan!");
            return;
        }

        RouteManager.Instance.ClearRoute();
        RouteManager.Instance.DrawRouteToBuilding(buildingId);

        Debug.Log("Navigating to: " + buildingId);
    }

    // OPTIONAL: kalau kamu mau saat masuk collider target yang sedang dinavigasi,
    // status navigating hilang (kembali start atau done tergantung unlockBuilding).
    // Panggil method ini dari BuildingTrigger.OnTriggerEnter:
    // PlayerTaskInstance.OnReachedBuilding(buildingId);
    public void OnReachedBuilding(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId)) return;
        if (buildingId != activeTargetId) return;

        activeTargetId = "";
        RefreshQuestButtons();

        // optional: kalau mau clear route saat sampai
        if (RouteManager.Instance != null)
            RouteManager.Instance.ClearRoute();
    }

    private void EndLoadingUI()
    {
        if (loading != null) loading.SetActive(false);
        if (mamah != null) mamah.SetActive(true);
    }

    private void OnPlayFabError(PlayFabError error)
    {
        Debug.LogError("Terjadi Error pada PlayFab: " + error.GenerateErrorReport());
    }
}
