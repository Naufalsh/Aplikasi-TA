using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;

public class EventQuizManager : MonoBehaviour
{
    [Header("UI References")]
    public Text questionText;
    public Toggle[] optionToggles;

    [Header("Progress UI")]
    // public Image progressFill;
    public GameObject finishConfirmationPanel;
    public GameObject resultPanel;
    public TextMeshProUGUI totalBenar;
    public TextMeshProUGUI totalPoin;

    [Header("Navigation Buttons")]
    public Button prevButton;
    public Button nextButton;
    public Button submitButton;
    public TextMeshProUGUI questionCounterText;

    private List<EventQuest> questions = new();

    private int currentQuestionIndex = 0;
    private int score = 0;

    private const int POINT_PER_CORRECT = 1;

    private readonly Dictionary<int, string> selectedAnswers = new();
    private readonly HashSet<int> correctSet = new();
    private readonly Dictionary<int, List<string>> shuffledOptionsCache = new();

    private string currentSessionId;

    private string leaderboardName = "Leaderboard";
    private string leaderboardAllTIme = "Leaderboard_AllTime";
    private string leaderboardSpesialEvent = "SpesialEvent";

    private Color defaultOptionTextColor;


    void Start()
    {
        currentSessionId = SystemInfo.deviceUniqueIdentifier;
        CheckSession();
        GetQuestSet();

        if (optionToggles != null && optionToggles.Length > 0)
    {
        var label = optionToggles[0].GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            defaultOptionTextColor = label.color;
        }
    }
    }

    // void GetQuestSet()
    // {
    //     EventDataQuiz currentEvent = GameManager.Instance.currentSelectedEvent;

    //     if (currentEvent == null)
    //     {
    //         Debug.LogError("Event belum dipilih.");
    //         return;
    //     }

    //     if (currentEvent.quiz == null ||
    //         currentEvent.quiz.quests == null ||
    //         currentEvent.quiz.quests.Count == 0)
    //     {
    //         Debug.LogError("Quiz event kosong.");
    //         return;
    //     }

    //     questions = currentEvent.quiz.quests;

    //     currentQuestionIndex = 0;
    //     score = 0;

    //     selectedAnswers.Clear();
    //     correctSet.Clear();
    //     shuffledOptionsCache.Clear();

    //     LoadQuestion();
    // }

    void GetQuestSet()
    {
        EventDataQuiz currentEvent = GameManager.Instance.currentSelectedEvent;

        if (currentEvent == null)
        {
            Debug.LogError("Event belum dipilih.");
            return;
        }

        if (GameManager.Instance.allEvents == null || GameManager.Instance.allEvents.Count == 0)
        {
            Debug.LogError("Daftar semua event kosong.");
            return;
        }

        string targetLocation = currentEvent.location;

        questions = GameManager.Instance.allEvents
            .Where(e =>
                e.location == targetLocation &&
                e.quiz != null &&
                e.quiz.quests != null &&
                e.quiz.quests.Count > 0)
            .SelectMany(e => e.quiz.quests)
            .ToList();

        if (questions.Count == 0)
        {
            Debug.LogError("Tidak ada quiz ditemukan untuk gedung ini.");
            return;
        }

        Debug.Log($"Total quiz digabung dari gedung {targetLocation}: {questions.Count}");

        currentQuestionIndex = 0;
        score = 0;

        selectedAnswers.Clear();
        correctSet.Clear();
        shuffledOptionsCache.Clear();

        LoadQuestion();
    }

    void LoadQuestion()
    {
        questionCounterText.text =
        $"Question {currentQuestionIndex + 1}/{questions.Count}";

        if (questions == null || questions.Count == 0)
            return;

        EventQuest q = questions[currentQuestionIndex];
        questionText.text = q.question;

        if (!shuffledOptionsCache.TryGetValue(currentQuestionIndex, out var opts))
        {
            opts = q.options.OrderBy(_ => Random.value).ToList();
            shuffledOptionsCache[currentQuestionIndex] = opts;
        }

        for (int i = 0; i < optionToggles.Length; i++)
        {
            if (i < opts.Count)
            {
                optionToggles[i].gameObject.SetActive(true);

                var label =
                    optionToggles[i].GetComponentInChildren<TextMeshProUGUI>();

                if (label != null)
                {
                    // Set teks opsi
                    label.text = opts[i];

                    // RESET warna teks ke warna default setiap kali soal baru dimuat
                    label.color = Color.black;
                }

                if (selectedAnswers.TryGetValue(currentQuestionIndex, out var saved))
                    optionToggles[i].isOn = saved == opts[i];
                else
                    optionToggles[i].isOn = false;

                // Aktifkan kembali toggle
                optionToggles[i].interactable = true;
            }
            else
            {
                optionToggles[i].gameObject.SetActive(false);
                optionToggles[i].isOn = false;
            }
        }

        UpdateNavigationButtons();

        // questionCounterText.text = $"Question {currentQuestionIndex + 1}/{questions.Count}";

        // if (questions == null || questions.Count == 0) return;

        // EventQuest q = questions[currentQuestionIndex];
        // questionText.text = q.question;

        // if (!shuffledOptionsCache.TryGetValue(currentQuestionIndex, out var opts))
        // {
        //     opts = q.options.OrderBy(_ => Random.value).ToList();
        //     shuffledOptionsCache[currentQuestionIndex] = opts;
        // }

        // for (int i = 0; i < optionToggles.Length; i++)
        // {
        //     if (i < opts.Count)
        //     {
        //         optionToggles[i].gameObject.SetActive(true);

        //         var label = optionToggles[i].GetComponentInChildren<TextMeshProUGUI>();
        //         if (label != null)
        //             label.text = opts[i];

        //         if (selectedAnswers.TryGetValue(currentQuestionIndex, out var saved))
        //             optionToggles[i].isOn = saved == opts[i];
        //         else
        //             optionToggles[i].isOn = false;
        //     }
        //     else
        //     {
        //         optionToggles[i].gameObject.SetActive(false);
        //         optionToggles[i].isOn = false;
        //     }
        // }

        // // UpdateProgress();
        // UpdateNavigationButtons();
        // for (int i = 0; i < optionToggles.Length; i++)
        // {
        //     optionToggles[i].interactable = true;
        // }
    }

    // void UpdateProgress()
    // {
    //     progressFill.fillAmount =
    //         (float)(currentQuestionIndex + 1) / questions.Count;
    // }

    void UpdateNavigationButtons()
    {
        bool isLastQuestion = currentQuestionIndex >= questions.Count - 1;

        prevButton.gameObject.SetActive(currentQuestionIndex > 0);
        nextButton.gameObject.SetActive(!isLastQuestion);
        submitButton.gameObject.SetActive(isLastQuestion);

        Debug.Log("Current Index: " + currentQuestionIndex);
        Debug.Log("Question Count: " + questions.Count);
        Debug.Log("Is Last Question: " + isLastQuestion);
    }

    public void NextQuestion()
    {
        SaveCurrentAnswer();

        ShowCorrectAnswer();

        // Nonaktifkan tombol agar tidak bisa ditekan berulang kali
        nextButton.interactable = false;
        prevButton.interactable = false;
        submitButton.interactable = false;

        StartCoroutine(NextQuestionAfterDelay(2f));

        // if (currentQuestionIndex < questions.Count - 1)
        // {
        //     currentQuestionIndex++;
        //     LoadQuestion();
        // }
    }

    public void PrevQuestion()
    {
        SaveCurrentAnswer();

        if (currentQuestionIndex > 0)
        {
            currentQuestionIndex--;
            LoadQuestion();
        }
    }

    void SaveCurrentAnswer()
    {
        string selected = GetSelectedAnswer();

        if (selected != null)
            selectedAnswers[currentQuestionIndex] = selected;
        else
            selectedAnswers.Remove(currentQuestionIndex);

        EvaluateCurrentAnswer(selected);
    }

    string GetSelectedAnswer()
    {
        foreach (var toggle in optionToggles)
        {
            if (toggle.isOn)
            {
                var label = toggle.GetComponentInChildren<TextMeshProUGUI>();

                if (label != null)
                    return label.text;
            }
        }

        return null;
    }

    void EvaluateCurrentAnswer(string selected)
    {
        string correctAnswer = questions[currentQuestionIndex].answer;

        bool wasCorrect = correctSet.Contains(currentQuestionIndex);
        bool isNowCorrect = selected != null && selected == correctAnswer;

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
    }

    public void ShowFinishConfirmation()
    {
        // Simpan jawaban terakhir
        SaveCurrentAnswer();

        // Tampilkan jawaban yang benar
        ShowCorrectAnswer();

        // Nonaktifkan semua tombol agar tidak bisa ditekan berulang
        nextButton.interactable = false;
        prevButton.interactable = false;
        submitButton.interactable = false;

        // Tunda 2 detik sebelum menampilkan panel konfirmasi selesai
        StartCoroutine(ShowFinishConfirmationAfterDelay(2f));
        // finishConfirmationPanel.SetActive(true);
    }

    public void HideFinishConfirmation()
    {
        finishConfirmationPanel.SetActive(false);
    }

    public void FinishQuiz()
    {
        // SaveCurrentAnswer();

        // HideFinishConfirmation();

        // totalBenar.text = correctSet.Count.ToString();
        // totalPoin.text = score.ToString();

        // resultPanel.SetActive(true);

        // SubmitScore(score);

        // StartCoroutine(ReturnToMap());

         // Simpan jawaban terakhir
    // Simpan jawaban terakhir
        SaveCurrentAnswer();

        // Tutup panel konfirmasi
        HideFinishConfirmation();

        // Isi data hasil quiz terlebih dahulu
        totalBenar.text = correctSet.Count.ToString();
        totalPoin.text = score.ToString();

        // Tampilkan jawaban benar (teks hijau) pada soal terakhir
        ShowCorrectAnswer();

        // Nonaktifkan tombol navigasi selama jeda
        nextButton.interactable = false;
        prevButton.interactable = false;
        submitButton.interactable = false;

        // Tunggu 2 detik, lalu tampilkan result panel
        StartCoroutine(FinishQuizAfterDelay(2f));
    }

    void SubmitScore(int reward)
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
                },
                new StatisticUpdate
                {
                    StatisticName = leaderboardSpesialEvent,
                    Value = score
                }
            },

        };
        PlayFabClientAPI.AddUserVirtualCurrency(
            new AddUserVirtualCurrencyRequest
            {
                VirtualCurrency = "CO",
                Amount = reward
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
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
            });
    }

    void CheckSession()
    {
        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest(),
            result =>
            {
                if (result.Data != null &&
                    result.Data.ContainsKey("deviceSession"))
                {
                    string sessionFromServer =
                        result.Data["deviceSession"].Value;

                    if (sessionFromServer != currentSessionId)
                    {
                        SceneManager.LoadScene("Main Menu");
                    }
                }
            },
            error =>
            {
                Debug.LogError(error.GenerateErrorReport());
            });
    }

    System.Collections.IEnumerator ReturnToMap()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("Gameplay");
    }

    private System.Collections.IEnumerator NextQuestionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Aktifkan kembali tombol
        nextButton.interactable = true;
        prevButton.interactable = true;
        submitButton.interactable = true;

        // Pindah ke soal berikutnya
        if (currentQuestionIndex < questions.Count - 1)
        {
            currentQuestionIndex++;
            LoadQuestion();
        }
    }


    // TAMBAHKAN method baru ini di dalam class EventQuizManager
    private void ShowCorrectAnswer()
    {
        if (questions == null || questions.Count == 0)
            return;

        string correctAnswer = questions[currentQuestionIndex].answer;
        string selectedAnswer = GetSelectedAnswer(); // Simpan jawaban yang dipilih pemain

        for (int i = 0; i < optionToggles.Length; i++)
        {
            Toggle toggle = optionToggles[i];

            if (!toggle.gameObject.activeSelf)
                continue;

            // Ambil komponen teks dari toggle
            var label = toggle.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null)
                continue;

            string optionText = label.text;

            // Pertahankan pilihan pemain (jangan ubah toggle yang dipilih)
            if (selectedAnswer != null)
            {
                toggle.isOn = (optionText == selectedAnswer);
            }

            // Jika ini jawaban yang benar, ubah warna teks menjadi hijau
            if (optionText == correctAnswer)
            {
                label.color = Color.green;
            }
            else
            {
                // Reset warna teks untuk opsi lainnya
                label.color = Color.black;
            }

            // Nonaktifkan toggle selama 2 detik agar tidak bisa diubah
            toggle.interactable = false;
        }
    }

    private System.Collections.IEnumerator ShowFinishConfirmationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Aktifkan kembali tombol
        nextButton.interactable = true;
        prevButton.interactable = true;
        submitButton.interactable = true;

        // Tampilkan panel konfirmasi finish
        finishConfirmationPanel.SetActive(true);
    }

private System.Collections.IEnumerator FinishQuizAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);

    // Tampilkan panel hasil
    resultPanel.SetActive(true);

    // Kirim skor ke PlayFab
    SubmitScore(score);

    // Kembali ke map setelah 3 detik
    StartCoroutine(ReturnToMap());
}
}