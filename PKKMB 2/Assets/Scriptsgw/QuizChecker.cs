using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class QuizChecker : MonoBehaviour
{
    public Button startQuizButton;   // tombol Start Quiz (drag dari Inspector)
    private string currentBuildingId;
    

    // Dipanggil saat player mau mulai quiz dari gedung tertentu
    public void PrepareQuiz(string buildingId)
    {
        currentBuildingId = buildingId;
        string key = "quiz_done_building_" + buildingId;

        // Cek status ke PlayFab
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            (result) =>
            {
                if (result.Data != null && result.Data.ContainsKey(key) && result.Data[key].Value == "true")
                {
                    Debug.Log("❌ Quiz gedung " + buildingId + " sudah dikerjakan!");
                    startQuizButton.interactable = false;
                }
                else
                {
                    Debug.Log("✅ Quiz gedung " + buildingId + " masih bisa dikerjakan.");
                    startQuizButton.interactable = true;

                    // Reset onclick lama biar tidak dobel
                    startQuizButton.onClick.RemoveAllListeners();

                    // Tambahkan action klik tombol → benar-benar cek lagi sebelum buka quiz
                    startQuizButton.onClick.AddListener(() =>
                    {
                        StartQuiz(buildingId);
                    });
                }
            },
            OnError
        );
    }

    // Saat user klik tombol Start
    private void StartQuiz(string buildingId)
    {
        string key = "quiz_done_building_" + buildingId;

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            (result) =>
            {
                if (result.Data != null && result.Data.ContainsKey(key) && result.Data[key].Value == "true")
                {
                    Debug.Log("⛔ Tidak bisa buka quiz. Sudah selesai sebelumnya!");
                    startQuizButton.interactable = false;
                }
                else
                {
                    Debug.Log("▶ Membuka quiz untuk gedung " + buildingId);
                    OpenQuiz(buildingId);
                }
            },
            OnError
        );
    }

    // Panggil ini setelah quiz selesai
    public void MarkQuizAsDone()
    {
        if (string.IsNullOrEmpty(currentBuildingId)) return;

        string key = "quiz_done_building_" + currentBuildingId;

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { key, "true" } }
        };

        PlayFabClientAPI.UpdateUserData(request,
            (result) =>
            {
                Debug.Log("✔ Quiz gedung " + currentBuildingId + " ditandai selesai.");
                startQuizButton.interactable = false;
            },
            OnError
        );
    }

    // TODO: implementasi tampilkan soal quiz
    private void OpenQuiz(string buildingId)
    {
        Debug.Log("📖 Load soal quiz sesuai gedung ID: " + buildingId);
        // misal load JSON berdasarkan buildingId
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError("PlayFab Error: " + error.GenerateErrorReport());
    }
}
