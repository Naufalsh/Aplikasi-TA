using Mapbox.Unity.MeshGeneration.Factories;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class BuildingTrigger : MonoBehaviour
{
    [SerializeField] public string buildingId;
    private GameObject questionMark;
    private string namaGedung;

    DirectionsFactory directionsFactory;

    void Awake()
    {
        directionsFactory = FindObjectOfType<DirectionsFactory>();
    }

    void Start()
    {
        GetNameBuilding();
        // Cari child bernama "QuestionMark" dari building ini

        Transform qmTransform = transform.Find("QuestionMark");
        if (qmTransform != null)
        {
            questionMark = qmTransform.gameObject;
            questionMark.SetActive(false); // Pastikan awalnya mati
        }
        else
        {
            Debug.LogWarning($"Building tidak punya child QuestionMark!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Player")) return;

        DailyQuestTask.Instance?.TryAutoCheckin(buildingId);

        // Aktifkan question mark
        if (questionMark != null)
        {
            questionMark.SetActive(true);
            Debug.Log($"Question mark untuk {buildingId} ditampilkan");

            if (QuestionMarkManager.Instance != null)
            {
                QuestionMarkManager.Instance.currentBuildingId = buildingId;
                // CheckQuizStatus(); //ini bikin error
            }
            UpdateBuildingVisit(namaGedung);
        }

        if (buildingId == directionsFactory.targetId)
        {
            directionsFactory.ClearRoute();
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Aktifkan question mark
        if (questionMark != null)
        {
            questionMark.SetActive(false);
            Debug.Log($"Question mark untuk {buildingId} ditutup");
        }
    }

    public void UpdateBuildingVisit(string buildingId)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "UpdateBuildingVisit", // nama handler yang ada di CloudScript
            FunctionParameter = new { buildingId = buildingId }
        };

        PlayFabClientAPI.ExecuteCloudScript(request, result =>
        {
            Debug.Log("Success: " + result.FunctionResult);
        },
        error =>
        {
            Debug.LogError("Error: " + error.GenerateErrorReport());
        });
    }

    private void GetNameBuilding()
    {
        if (GameManager.Instance != null && GameManager.Instance.buildingCache.ContainsKey(buildingId))
        {
            BuildingData targetBuilding = GameManager.Instance.buildingCache[buildingId];

            // Isi panel Info sesuai gedung
            namaGedung = targetBuilding.name;

        }
    }

}
