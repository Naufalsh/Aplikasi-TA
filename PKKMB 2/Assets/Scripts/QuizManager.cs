using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using Mapbox.Json;
using UnityEngine.SceneManagement;
using TMPro;
using System;


[System.Serializable]
public class QuestSet
{
    public int id;
    public string name;
    public List<Quests> quests;
}

[System.Serializable]
public class Quests
{
    public string questId;
    public string question;
    public List<string> options;
    public string answer;
}

public class QuizManager : MonoBehaviour
{
    [Header("UI References")]
    public Text questionText;
    public Toggle[] optionToggles;

    [Header("Progress UI")]
    public Image progressFill;
    public GameObject finishConfirmationPanel;
    public GameObject resultPanel;
    public TextMeshProUGUI totalBenar;
    public TextMeshProUGUI totalPoin;

    public Button prevButton;
    public Button nextButton;
    public Button submitButton;


    private List<Quests> questions;
    private int currentQuestionIndex = 0;


    private int score = 0;
    private const int POINT_PER_CORRECT = 20;


    private readonly Dictionary<int, string> selectedAnswers = new Dictionary<int, string>();

    private readonly HashSet<int> correctSet = new HashSet<int>();


    private readonly Dictionary<int, List<string>> shuffledOptionsCache = new Dictionary<int, List<string>>();

    public string IdQuest;
    public QuestSet questSet;

    private string currentSessionId;
    private string leaderboardName = "Leaderboard";
    private string leaderboardAllTIme = "Leaderboard_AllTime";

    public Button tombolMulaiQuiz;
    public Text teksTombol;

    void Start()
    {
        currentSessionId = SystemInfo.deviceUniqueIdentifier;
        CheckSession();
        if (QuestionMarkManager.Instance != null)
        {
            IdQuest = QuestionMarkManager.Instance.currentBuildingId;
            Debug.Log("📌 Quiz untuk gedung: " + IdQuest);
        }
        else
        {
            Debug.LogWarning("⚠ QuestionMarkManager tidak ditemukan, IdQuest kosong.");
        }
        GetQuestSet();

        // currentSessionId = SystemInfo.deviceUniqueIdentifier;
        // CheckSession();

        // ambil ID gedung dari GameManager
        // if (QuestionMarkManager.Instance != null)
        // {
        //     IdQuest =QuestionMarkManager.Instance.currentBuildingId;
        //     Debug.Log("📌 Quiz untuk gedung: " + IdQuest);
        // }
        // else
        // {
        //     Debug.LogWarning("GameManager tidak ditemukan, IdQuest kosong.");
        // }

        // GetQuestSet();
    }

    void CheckSession()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data != null && result.Data.ContainsKey("deviceSession"))
            {
                string sessionFromServer = result.Data["deviceSession"].Value;

                if (sessionFromServer != currentSessionId)
                {
                    Debug.LogWarning("Session tidak valid. User login dari device lain.");
                    SceneManager.LoadScene("Main Menu");
                }
            }
        },
        error => Debug.LogError("Gagal ambil session: " + error.GenerateErrorReport()));
    }

    void LoadQuestion()
    {
        if (questions == null || questions.Count == 0)
            return;

        var q = questions[currentQuestionIndex];
        questionText.text = q.question;


        if (!shuffledOptionsCache.TryGetValue(currentQuestionIndex, out var opts))
        {
            opts = q.options.OrderBy(_ => UnityEngine.Random.value).ToList();
            shuffledOptionsCache[currentQuestionIndex] = opts;
        }


        for (int i = 0; i < optionToggles.Length; i++)
        {
            if (i < opts.Count)
            {
                var label = optionToggles[i].GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = opts[i];

                optionToggles[i].gameObject.SetActive(true);


                if (selectedAnswers.TryGetValue(currentQuestionIndex, out var saved))
                    optionToggles[i].isOn = (saved == opts[i]);
                else
                    optionToggles[i].isOn = false;
            }
            else
            {
                optionToggles[i].gameObject.SetActive(false);
                optionToggles[i].isOn = false;
            }
        }

        UpdateProgress();
    }

    void UpdateProgress()
    {
        float progressValue = (float)(currentQuestionIndex + 1) / questions.Count;
        progressFill.fillAmount = progressValue;
    }

    public void NextQuestion()
    {

        string selected = null;
        foreach (var t in optionToggles)
        {
            if (t.isOn)
            {
                var lbl = t.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) selected = lbl.text;
                break;
            }
        }

        string correct = questions[currentQuestionIndex].answer;

        bool wasCorrect = correctSet.Contains(currentQuestionIndex);
        bool isNowCorrect = (selected != null && selected == correct);

        if (isNowCorrect && !wasCorrect)
        {
            score += POINT_PER_CORRECT;
            correctSet.Add(currentQuestionIndex);
        }
        else if (!isNowCorrect && wasCorrect)
        {
            score -= POINT_PER_CORRECT;
            correctSet.Remove(currentQuestionIndex);
        }



        if (selected != null)
            selectedAnswers[currentQuestionIndex] = selected;
        else
            selectedAnswers.Remove(currentQuestionIndex);


        if (currentQuestionIndex < questions.Count - 1)
        {
            currentQuestionIndex++;
            LoadQuestion();
        }
        else
        {
            ShowFinishConfirmation();
        }
    }

    public void PrevQuestion()
    {
        if (currentQuestionIndex > 0)
        {
            currentQuestionIndex--;
            LoadQuestion();
        }
    }

    void GetQuestSet()
    {
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), result =>
        {
            if (result.Data != null && result.Data.ContainsKey("ListQuiz"))
            {
                string json = result.Data["ListQuiz"];

                var allQuests = JsonConvert.DeserializeObject<Dictionary<string, QuestSet>>(json);
                if (allQuests.ContainsKey(IdQuest))
                {
                    questSet = allQuests[IdQuest];


                    questions = questSet.quests;


                    currentQuestionIndex = 0;
                    score = 0;
                    selectedAnswers.Clear();
                    correctSet.Clear();
                    shuffledOptionsCache.Clear();

                    LoadQuestion();
                }
                else
                {
                    Debug.LogWarning($"Quiz ID '{IdQuest}' tidak ditemukan di Title Data.");
                }
            }
            else
            {
                Debug.LogWarning("TitleData 'ListQuiz' kosong atau tidak ditemukan.");
            }
        },
        error => Debug.LogError(error.GenerateErrorReport()));
    }

    public void ShowFinishConfirmation()
    {
        finishConfirmationPanel?.SetActive(true);
        Debug.Log($"✅ Kuis selesai. Skor akhir: {score}");
    }

    public void HideFinishConfirmation()
    {
        finishConfirmationPanel?.SetActive(false);
    }

    public void FinishQuiz()
    {
        // NextQuestion(); // <-- HAPUS INI (Saran dari perbaikan sebelumnya agar tidak double count)
        
        Debug.Log("✅ Quiz Finished! Reload scene…");
        SubmitScore(score);
        
        // --- PERBAIKAN DI SINI ---
        // Jangan pakai score / 5. Gunakan jumlah data di correctSet.
        totalBenar.text = correctSet.Count.ToString(); 
        // -------------------------

        totalPoin.text = score.ToString();
        resultPanel.SetActive(true);
        StartCoroutine(ChangeSceneAfterDelay(3f));
        
        Debug.Log("total score = " + score);
        
        MarkQuizAsCompleted(IdQuest, () =>
        {
            Debug.Log("Quiz berhasil ditandai sebagai selesai!");
        });
    



    }

    private System.Collections.IEnumerator ChangeSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("Gameplay");
    }

    public void SubmitScore(int score)
    {
        CheckSession();
        var statRequest = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = leaderboardName,
                    Value = score
                },
                new StatisticUpdate
                {
                    StatisticName = leaderboardAllTIme,
                    Value = score
                }
            },
            
        };

        PlayFabClientAPI.AddUserVirtualCurrency(new PlayFab.ClientModels.AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = "CO",
            Amount = score
        },
        result =>
        {
            PlayFabClientAPI.UpdatePlayerStatistics(statRequest,
            result =>
                {
                    Debug.Log("Skor berhasil dikirim ke PlayFab!");
                },
                error => Debug.LogError("Gagal kirim skor: " + error.GenerateErrorReport())
            );
            Debug.Log($"Berhasil menambahkan {score} koin. Total koin sekarang: {result.Balance}");
        },
        error => Debug.LogError("Gagal menambahkan koin: " + error.GenerateErrorReport()));
    }

    void OnError(PlayFabError error)
    {
        Debug.LogError("PlayFab Error: " + error.GenerateErrorReport());
    }



    //Hal-hal Baru

    // Panggil fungsi ini SETELAH pemain berhasil menyelesaikan kuis
    public void MarkQuizAsCompleted(string buildingId, Action onComplete)
    {
        CheckSession(); // Pastikan sesi PlayFab aktif

        // 1. Ambil data lama dari PlayFab
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            string currentData = "";
            // Cek apakah data "completedQuizzes" sudah ada
            if (result.Data != null && result.Data.ContainsKey("completedQuizzes"))
            {
                currentData = result.Data["completedQuizzes"].Value;
            }

            // 2. Ubah string menjadi List dan tambahkan ID gedung baru
            List<string> completedList = new List<string>(currentData.Split(','));

            // Tambahkan hanya jika belum ada di dalam list
            if (!completedList.Contains(buildingId))
            {
                completedList.Add(buildingId);
            }

            // 3. Gabungkan kembali menjadi string
            // string.Join akan menangani list kosong atau berisi satu item dengan benar
            string updatedData = string.Join(",", completedList.Where(s => !string.IsNullOrEmpty(s)));

            // 4. Kirim data baru ke PlayFab
            var updateRequest = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
                {
                { "completedQuizzes", updatedData }
                }
            };

            PlayFabClientAPI.UpdateUserData(updateRequest, updateResult =>
            {
                Debug.Log("Berhasil menyimpan progres kuis: " + updatedData);
                onComplete?.Invoke(); // Jalankan callback jika ada (misal: untuk update UI)
            },
            error =>
            {
                Debug.LogError("Gagal menyimpan progres kuis: " + error.GenerateErrorReport());
            });
        },
        error =>
        {
            Debug.LogError("Gagal mengambil data kuis: " + error.GenerateErrorReport());
        });
    }

    public void CheckQuizStatus(string buildingId)
    {
        CheckSession();

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            bool isCompleted = false;
            if (result.Data != null && result.Data.ContainsKey("completedQuizzes"))
            {
                string currentData = result.Data["completedQuizzes"].Value;
                List<string> completedList = new List<string>(currentData.Split(','));

                // Cek apakah ID gedung ini ada di dalam daftar yang sudah selesai
                if (completedList.Contains(buildingId))
                {
                    isCompleted = true;
                }
            }

            // Update tampilan tombol berdasarkan status
            UpdateQuizButtonUI(isCompleted);

        },
        error =>
        {
            Debug.LogError("Gagal memeriksa status kuis: " + error.GenerateErrorReport());
            // Jika gagal, mungkin amannya non-aktifkan tombol saja
            tombolMulaiQuiz.interactable = false;
            teksTombol.text = "Error";
        });
    }

    // Fungsi bantuan untuk mengubah tampilan tombol
    private void UpdateQuizButtonUI(bool isCompleted)
    {
        if (isCompleted)
        {
            // Jika sudah selesai
            teksTombol.text = "SELESAI";
            tombolMulaiQuiz.interactable = false; // Tombol tidak bisa diklik lagi
        }
        else
        {
            // Jika belum dikerjakan
            teksTombol.text = "MULAI KUIS";
            tombolMulaiQuiz.interactable = true; // Tombol bisa diklik
        }
    }

    
}