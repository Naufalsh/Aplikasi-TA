using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using TMPro;

public class GameModeManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI activeModeText;
    public GameObject loadingPanel;
    private PlayerTask playerTask;

    [Header("PlayFab Settings")]
    // Pastikan key ini sesuai dengan di PlayFab (huruf besar/kecilnya)
    private string titleDataKey = "Mode";
    private SpecialEventTask specialEventTask;
    private DailyQuestTask dailyQuestTask;

    void Start()
    {
        playerTask = FindObjectOfType<PlayerTask>();
        specialEventTask = FindObjectOfType<SpecialEventTask>();
        dailyQuestTask = FindObjectOfType<DailyQuestTask>();

        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            GetGameModeData();
        }
        else
        {
            Debug.LogError("User belum login!");
        }
    }

    public void GetGameModeData()
    {
        // 1. NYALAKAN LOADING (Start)
        if (loadingPanel != null) loadingPanel.SetActive(true);

        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest
        {
            Keys = new List<string> { titleDataKey }
        }, OnGetTitleDataSuccess, OnError);
    }

    void OnGetTitleDataSuccess(GetTitleDataResult result)
    {
        // ⚠ PENTING: JANGAN MATIKAN LOADING DISINI!
        // Kita belum tahu apakah proses benar-benar selesai atau butuh ambil data lagi.

        if (result.Data == null || !result.Data.ContainsKey(titleDataKey))
        {
            // Kalau data error/kosong, baru terpaksa matikan loading
            if (loadingPanel != null) loadingPanel.SetActive(false);
            return;
        }

        string jsonString = result.Data[titleDataKey];
        GameModeData data = JsonUtility.FromJson<GameModeData>(jsonString);

        if (activeModeText != null)
        {
            string displayText = "";

            switch (data.Active)
            {
                // Di dalam GameModeManager.cs -> OnGetTitleDataSuccess

                case "Default":
                    displayText = "Daily Quest";
                    activeModeText.text = displayText;

                    // Panggil script DailyQuestTask
                    if (dailyQuestTask != null)
                    {
                        // Loading tetap nyala, matikan setelah proses daily selesai
                        dailyQuestTask.LoadDailyQuest(() =>
                        {
                            if (loadingPanel != null) loadingPanel.SetActive(false);
                        });
                    }
                    else
                    {
                        // Fallback jika script lupa dipasang
                        if (loadingPanel != null) loadingPanel.SetActive(false);
                    }
                    break;

                case "SpecialEvent":
                    displayText = "Special Event";
                    activeModeText.text = displayText;

                    if (specialEventTask != null)
                    {
                        // Case SpecialEvent: Butuh ambil data lagi (fetch kedua)
                        // Loading BIARKAN MENYALA. Kita titip pesan ke script task:
                        // "Nanti kalau kamu sudah selesai, tolong matikan loading panel saya ya"
                        specialEventTask.LoadSpecialEventData(() =>
                        {
                            // 2. MATIKAN LOADING (Finish) - Hanya dimatikan disini
                            if (loadingPanel != null) loadingPanel.SetActive(false);
                        });
                    }
                    else
                    {
                        // Fallback jika script hilang
                        if (loadingPanel != null) loadingPanel.SetActive(false);
                    }
                    break;

                case "PKKMB":
                    displayText = "Quest Group";
                    activeModeText.text = displayText;

                    // Case PKKMB: Loading Utama dimatikan, karena PlayerTask punya loading sendiri ("mamah")
                    // Jadi ini serah terima jabatan loading.
                    if (loadingPanel != null) loadingPanel.SetActive(false);

                    if (playerTask != null)
                    {
                        playerTask.LoadPKKMBQuests();
                    }
                    break;

                default:
                    displayText = data.Active;
                    activeModeText.text = displayText;
                    // Mode tidak dikenal -> Loading Mati
                    if (loadingPanel != null) loadingPanel.SetActive(false);
                    break;
            }
        }
    }

    void OnError(PlayFabError error)
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        Debug.LogError("Error: " + error.GenerateErrorReport());
    }
}

[System.Serializable]
public class GameModeData
{
    public string Active;
    public string[] listMode;
}