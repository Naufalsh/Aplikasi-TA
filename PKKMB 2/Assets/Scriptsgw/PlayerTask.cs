using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using Mapbox.Unity.MeshGeneration.Factories;

[Serializable]
public class Questaw
{
    // DIUBAH: 'isUnlocked' dihapus karena pengecekan dilakukan di C#
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

    [Header("Quest Start Buttons")]
    public GameObject start1;
    public GameObject start2;
    public GameObject start3;

    private AbstractMap map;
    private Transform player;
    private DirectionsFactory directionsFactory;
    private List<BuildingTrigger> sceneBuildings;
    private List<GoldenQuestTrigger> sceneNonBuildings;
    
    // BARU: Variabel untuk menyimpan daftar ID gedung yang sudah dibuka
    private List<string> unlockedBuildingIds = new List<string>();
    public GameObject mamah;
    public GameObject loading;

    public GameObject done1;
    public GameObject done2;
    public GameObject done3;

        private void Awake()
    {
        map = FindObjectOfType<AbstractMap>();
        GameObject playerObj = GameObject.Find("PlayerTarget");
        if (playerObj != null) player = playerObj.transform;
        directionsFactory = FindObjectOfType<DirectionsFactory>();

        if (map == null || player == null || directionsFactory == null)
            Debug.LogError("Komponen routing (Map, Player, or DirectionsFactory) tidak ditemukan!");
    }

    private void Start()
    {
        sceneNonBuildings = new List<GoldenQuestTrigger>(FindObjectsOfType<GoldenQuestTrigger>());
        sceneBuildings = new List<BuildingTrigger>(FindObjectsOfType<BuildingTrigger>());
        Debug.Log($"Ditemukan {sceneBuildings.Count} gedung di scene.");

        // Memulai alur pengambilan data secara berantai
    }

    public void LoadPKKMBQuests()
    {
        // Pastikan loading UI muncul sebelum proses dimulai
        if (loading != null) loading.SetActive(true);
        if (mamah != null) mamah.SetActive(false);
        
        Debug.Log("GameModeManager memanggil loading quest PKKMB.");
        StartQuestLoadingProcess();
    }

    // --- ALUR PENGAMBILAN DATA BERANTAI ---

    // Langkah 1: Ambil GroupNumber pemain
    private void StartQuestLoadingProcess()
    {
        Debug.Log("Langkah 1: Mengambil GroupNumber pemain...");
        PlayFabClientAPI.GetUserData(new GetUserDataRequest { Keys = new List<string> { "GroupNumber" } },
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey("GroupNumber"))
                {
                    string groupStr = result.Data["GroupNumber"].Value;
                    if (int.TryParse(groupStr, out int groupNumber))
                    {
                        // DIUBAH: Jika berhasil, panggil Langkah 2 dan teruskan groupNumber
                        GetUnlockBuilding(groupNumber);
                    }
                    else
                    {
                        Debug.LogError("Gagal parse GroupNumber dari UserData: " + groupStr);
                    }
                }
                else
                {
                    Debug.LogWarning("Pemain tidak memiliki 'GroupNumber' di UserData.");
                }
            },
            OnPlayFabError);
    }

    // Langkah 2: Ambil data unlockBuilding
    private void GetUnlockBuilding(int groupNumber)
    {
        Debug.Log("Langkah 2: Mengambil data unlockBuilding...");
        PlayFabClientAPI.GetUserData(new GetUserDataRequest { Keys = new List<string> { "unlockBuilding" } },
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey("unlockBuilding"))
                {
                    string data = result.Data["unlockBuilding"].Value;
                    unlockedBuildingIds = new List<string>(data.Split(','));
                    Debug.Log("Unlocked Buildings Loaded: " + string.Join(", ", unlockedBuildingIds));
                }
                else
                {
                    unlockedBuildingIds.Clear(); // Pastikan list kosong jika tidak ada data
                    Debug.Log("No unlockBuilding data found.");
                }

                // DIUBAH: Jika berhasil (atau tidak ada data), panggil Langkah 3
                FetchQuestsForGroup(groupNumber);
            },
            OnPlayFabError);
    }

    // Langkah 3: Ambil data quest dari CloudScript
    private void FetchQuestsForGroup(int groupNumber)
    {
        Debug.Log($"Langkah 3: Meminta quest untuk grup {groupNumber} dari CloudScript...");
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "getQuestsByGroup",
            FunctionParameter = new { groupCode = groupNumber }
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnFetchQuestsSuccess, OnPlayFabError);
    }

    // Langkah 4: Terima hasil quest dan panggil UI update
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
                    // Langkah terakhir: Terapkan ke UI
                    ApplyQuestToUI(wrapper.quests);
                }
                else
                {
                    Debug.LogError("Hasil JSON dari CloudScript tidak valid atau tidak berisi 'quests'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Gagal parse JSON dari CloudScript: {e.Message}. JSON diterima: {result.FunctionResult.ToString()}");
            }
        }
        else
        {
            Debug.LogWarning("CloudScript berhasil dieksekusi tetapi tidak mengembalikan data. Mungkin grup tidak ditemukan.");
        }
    }

    // Fungsi untuk menampilkan quest ke UI
    private void ApplyQuestToUI(List<Questaw> quests)
    {
        if (quests == null) return;
        Debug.Log("Langkah 5: Menerapkan quest ke UI dan melakukan pengecekan unlock status.");

        // Setup Quest 1
        if (quests.Count > 0)
        {
            title1.text = quests[0].Destination;
            description1.text = "Find the " + quests[0].Destination + " To complete the quests!";
            start1.SetActive(true);
            Button button1 = start1.GetComponent<Button>();
            if (button1 != null)
            {
                // DIUBAH: Lakukan pengecekan langsung di sini
                start1.SetActive(!unlockedBuildingIds.Contains(quests[0].buildingId));
                done1.SetActive(unlockedBuildingIds.Contains(quests[0].buildingId));
                button1.onClick.RemoveAllListeners();
                button1.onClick.AddListener(() => ShowRouteToBuilding(quests[0].buildingId));
            }
        }
        // ... (Lakukan hal yang sama untuk quest 2 dan 3)
        if (quests.Count > 1)
        {
            title2.text = quests[1].Destination;
            description2.text = "Find the " + quests[1].Destination + " To complete the quests!";
            start2.SetActive(true);
            Button button2 = start2.GetComponent<Button>();
            if (button2 != null)
            {
                start2.SetActive(!unlockedBuildingIds.Contains(quests[1].buildingId));
                done2.SetActive(unlockedBuildingIds.Contains(quests[1].buildingId));
                button2.onClick.RemoveAllListeners();
                button2.onClick.AddListener(() => ShowRouteToBuilding(quests[1].buildingId));
            }
        }
        if (quests.Count > 2)
        {
            title3.text = quests[2].Destination;
            description3.text = "Find the " + quests[2].Destination + " To complete the quests!";
            start3.SetActive(true);
            Button button3 = start3.GetComponent<Button>();
            if (button3 != null)
            {
                start3.SetActive(!unlockedBuildingIds.Contains(quests[2].buildingId));
                done3.SetActive(unlockedBuildingIds.Contains(quests[2].buildingId));
                button3.onClick.RemoveAllListeners();
                button3.onClick.AddListener(() => ShowRouteToBuilding(quests[2].buildingId));
            }
        }
        mamah.SetActive(true);
        loading.SetActive(false);
    }
    
    // Handler Error Umum
    private void OnPlayFabError(PlayFabError error)
    {
        Debug.LogError("Terjadi Error pada PlayFab: " + error.GenerateErrorReport());
    }

    // Fungsi routing (tidak ada perubahan)
    public void ShowRouteToBuilding(string targetBuildingId)
    {
        Debug.Log("Mencoba menampilkan rute ke gedung ID: " + targetBuildingId);

        if (map == null || player == null || directionsFactory == null)
        {
            Debug.LogError("Referensi untuk routing hilang! Tidak dapat membuat rute.");
            return;
        }

        BuildingTrigger targetBuilding = sceneBuildings.FirstOrDefault(b => b.buildingId == targetBuildingId);
        GoldenQuestTrigger targetNonBuilding = sceneNonBuildings.FirstOrDefault(b => b.buildingId == targetBuildingId);

        if (targetBuilding != null)
        {
            // directionsFactory.SetRoute(player, targetBuilding.transform, targetBuilding.buildingId);
            // directionsFactory.ShowRoute();
        }
        else if (targetNonBuilding != null)
        {
            // directionsFactory.SetRoute(player, targetNonBuilding.transform, targetNonBuilding.buildingId);
            // directionsFactory.ShowRoute();
        }
        else
        {
            Debug.LogError("Tidak dapat menemukan BuildingTrigger/GoldenQuestTrigger di scene dengan ID: " + targetBuildingId);
        }
    }
}