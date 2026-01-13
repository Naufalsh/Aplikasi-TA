using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.SocialPlatforms.Impl;

public class QuestionMark : MonoBehaviour
{
    private string buildingId;
    BuildingTrigger trigger;

    // Panel Info
    private Button mulaiQuizButton;
    private GameObject infoPanel;
    private TextMeshProUGUI namaGedung;
    private TextMeshProUGUI namaGedung2;
    private TextMeshProUGUI infoGedung;
    private Image imageGedung;

    // Toggle label FindAround
    private TextMeshProUGUI listAround;
    private TextMeshProUGUI listAround2;
    private TextMeshProUGUI listAround3;

    public Button ButtonStartQuiz;
    public Image DoneQuiz;
    public TextMeshProUGUI StartText;
    private string currentSessionId;
    private Toggle toggle1;
    private Toggle toggle2;
    private Toggle toggle3;
    private Button submitButton;
    private TextMeshProUGUI textSubmitButton;
    private ListPageScript listPageScript;

    private string findTheBuildingTemplate =
    "Look around {BUILDING_NAME}, check the box below that you think is correct. Submit your answer to earn bonus points!";
    private string leaderboardName = "Leaderboard";
    private string leaderboardAllTIme = "Leaderboard_AllTime";


    private void Start()
    {
        currentSessionId = SystemInfo.deviceUniqueIdentifier;

        // Ambil BuildingTrigger dari parent
        trigger = GetComponentInParent<BuildingTrigger>();
        if (trigger != null)
        {
            buildingId = trigger.buildingId;
            CheckStatus();
        }
        else
        {
            Debug.LogError("❌ QuestionMark tidak menemukan BuildingTrigger di parent!");
        }

        // Cari panel Info
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject panel in allObjects)
        {
            if (panel.name == "Info")
            {
                infoPanel = panel;
                namaGedung = infoPanel.transform.Find("JudulGedung").GetComponent<TextMeshProUGUI>();
                infoGedung = infoPanel.transform.Find("Scroll View/Viewport/Content/InfoGedung").GetComponent<TextMeshProUGUI>();
                imageGedung = infoPanel.transform.Find("ImageGedung").GetComponent<Image>();

                ButtonStartQuiz = infoPanel.transform.Find("QuizSection/ButtonStartQuiz").GetComponent<Button>();
                StartText = infoPanel.transform.Find("QuizSection/ButtonStartQuiz/StartText").GetComponent<TextMeshProUGUI>();

                namaGedung2 = infoPanel.transform.Find("FindTheBuildingSection/Deskripsi Quiz").GetComponent<TextMeshProUGUI>();
                listAround = infoPanel.transform.Find("FindTheBuildingSection/Toggle/Label").GetComponent<TextMeshProUGUI>();
                listAround2 = infoPanel.transform.Find("FindTheBuildingSection/Toggle2/Label").GetComponent<TextMeshProUGUI>();
                listAround3 = infoPanel.transform.Find("FindTheBuildingSection/Toggle3/Label").GetComponent<TextMeshProUGUI>();

                toggle1 = infoPanel.transform.Find("FindTheBuildingSection/Toggle").GetComponent<Toggle>();
                toggle2 = infoPanel.transform.Find("FindTheBuildingSection/Toggle2").GetComponent<Toggle>();
                toggle3 = infoPanel.transform.Find("FindTheBuildingSection/Toggle3").GetComponent<Toggle>();
                submitButton = infoPanel.transform.Find("FindTheBuildingSection/SubmitButton").GetComponent<Button>();
                textSubmitButton = infoPanel.transform.Find("FindTheBuildingSection/SubmitButton/TextSubmit").GetComponent<TextMeshProUGUI>();
                if (submitButton != null)
                {
                    submitButton.onClick.RemoveAllListeners();
                    submitButton.onClick.AddListener(SubmitAnswer);
                }
                break;
            }
        }

        // Pastikan panel Info awalnya tidak aktif
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance != null && GameManager.Instance.buildingCache.ContainsKey(buildingId))
        {
            BuildingData targetBuilding = GameManager.Instance.buildingCache[buildingId];

            // Isi panel Info sesuai gedung
            namaGedung.text = targetBuilding.name;
            infoGedung.text = targetBuilding.description;
            namaGedung2.text = findTheBuildingTemplate.Replace("{BUILDING_NAME}", targetBuilding.name);
            SetGedungImage(targetBuilding.id);

            // Cek status kuis
            ButtonStartQuiz.onClick.RemoveAllListeners();
            ButtonStartQuiz.onClick.AddListener(CheckStatus);

            // 🔎 Ambil data FindAround dari cache
            FindAroundSet findAroundSet = FindAroundBuilding.Instance.GetFindAroundByBuilding(buildingId);
            if (findAroundSet != null && findAroundSet.quests != null && findAroundSet.quests.Count > 0)
            {
                var quest = findAroundSet.quests[0]; // Ambil quest pertama

                if (quest.options.Count > 0) listAround.text = quest.options[0];
                if (quest.options.Count > 1) listAround2.text = quest.options[1];
                if (quest.options.Count > 2) listAround3.text = quest.options[2];

                Debug.Log($"📝 Options untuk {buildingId}: {string.Join(", ", quest.options)}");
                Debug.Log($"✅ Answer untuk {buildingId}: {string.Join(", ", quest.answer)}");
            }
            else
            {
                Debug.LogWarning($"⚠ Tidak ada data FindAround untuk buildingId={buildingId}");
            }

            // Tampilkan panel
            infoPanel.SetActive(true);

            Debug.Log($"📌 Question mark diklik -> tampilkan info {buildingId}");
        }
        else
        {
            Debug.LogError($"❌ BuildingId {buildingId} tidak ditemukan di GameManager.buildingCache");
        }
    }

    private void SetGedungImage(string buildingId)
    {
        Sprite spriteGedung = Resources.Load<Sprite>("BuildingInfoImage/" + buildingId);
        if (spriteGedung != null)
        {
            imageGedung.sprite = spriteGedung;
        }
        else
        {
            Debug.LogWarning("⚠ Gambar tidak ditemukan untuk ID: " + buildingId);
        }
    }

    public void CheckStatus()
    {
        CheckSession();

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            bool isFindAroundCompleted = false;
            bool isQuizCompleted = false;
            if (result.Data != null && result.Data.ContainsKey("FindAround"))
            {
                string currentData = result.Data["FindAround"].Value;
                List<string> completedList = new List<string>(currentData.Split(','));

                // Cek apakah ID gedung ini sudah selesai
                if (completedList.Contains(buildingId))
                {
                    isFindAroundCompleted = true;
                }
            }

            if (result.Data != null && result.Data.ContainsKey("completedQuizzes"))
            {
                string currentData = result.Data["completedQuizzes"].Value;
                List<string> completedList = new List<string>(currentData.Split(','));

                // Cek apakah ID gedung ini ada di dalam daftar yang sudah selesai
                if (completedList.Contains(buildingId))
                {
                    isQuizCompleted = true;
                }
            }

            UpdateQuizButtonUI(isQuizCompleted);
            UpdateFindAroundButtonUI(isFindAroundCompleted);

            if (isQuizCompleted && isFindAroundCompleted)
            {
                Debug.Log(buildingId + "Yahii");
                AddUnlockBuilding(buildingId);
            }

        },
        error =>
        {
            Debug.LogError("❌ Gagal memeriksa status kuis: " + error.GenerateErrorReport());
        });
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
                    Debug.LogWarning("⚠ Session tidak valid. User login dari device lain.");
                    SceneManager.LoadScene("Main Menu");
                }
            }
        },
        error => Debug.LogError("❌ Gagal ambil session: " + error.GenerateErrorReport()));
    }

    void UpdateQuizButtonUI(bool isCompleted)
    {
        if (isCompleted)
        {
            // Jika sudah selesai
            StartText.text = "Done";
            ButtonStartQuiz.interactable = false; // Tombol tidak bisa diklik lagi
        }
        else
        {
            // Jika belum dikerjakan
            StartText.text = "Start";
            ButtonStartQuiz.interactable = true; // Tombol bisa diklik
        }
    }
    void UpdateFindAroundButtonUI(bool isCompleted)
    {
        if (isCompleted)
        {
            textSubmitButton.text = "Done";
            submitButton.interactable = false;
        }
        else
        {
            textSubmitButton.text = "Submit";
            submitButton.interactable = true;
        }
    }

    public void SubmitAnswer()
    {
        FindAroundSet findAroundSet = FindAroundBuilding.Instance.GetFindAroundByBuilding(buildingId);
        if (findAroundSet == null || findAroundSet.quests.Count == 0) return;

        var quest = findAroundSet.quests[0];

        // --- Langkah 1: Kumpulkan data dan hitung metrik ---
        List<string> selectedAnswers = new List<string>();
        if (toggle1 != null && toggle1.isOn) selectedAnswers.Add(listAround.text);
        if (toggle2 != null && toggle2.isOn) selectedAnswers.Add(listAround2.text);
        if (toggle3 != null && toggle3.isOn) selectedAnswers.Add(listAround3.text);

        List<string> correctAnswers = quest.answer;

        int correctSelected = selectedAnswers.Count(a => correctAnswers.Contains(a));
        int incorrectSelected = selectedAnswers.Count(a => !correctAnswers.Contains(a));
        int totalCorrectAnswersAvailable = correctAnswers.Count;

        int finalScore = 0; // Skor default adalah 0

        // --- Langkah 2: Logika Penilaian "Hitam-Putih" ---

        // Aturan #1: Jika ada kesalahan, skor langsung 0.
        if (incorrectSelected > 0)
        {
            finalScore = 0;
        }
        // Jika tidak ada kesalahan sama sekali, baru kita cek untuk skor 20 atau 10.
        else
        {
            // Aturan #2: Kondisi Sempurna
            if (correctSelected == totalCorrectAnswersAvailable && correctSelected > 0)
            {
                finalScore = 20;
            }
            // Aturan #3: Kondisi Hanya Satu Jawaban Benar
            else if (correctSelected == 1)
            {
                finalScore = 10;
            }
            // Aturan #4: Kondisi lain (misal: memilih 2 dari 3 benar) skor tetap 0.
        }

        Debug.Log($"📊 Skor dihitung: {finalScore} poin (Benar: {correctSelected}, Salah: {incorrectSelected})");

        // --- Langkah 3: Kirim hasil dan tandai kuis selesai ---
        if (finalScore > 0)
        {
            SubmitScore(finalScore);
        }

        MarkQuizAsCompleted();
    }


    void MarkQuizAsCompleted()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            List<string> completedList = new List<string>();

            if (result.Data != null && result.Data.ContainsKey("FindAround"))
            {
                completedList = new List<string>(result.Data["FindAround"].Value.Split(','));
            }

            if (!completedList.Contains(buildingId))
                completedList.Add(buildingId);

            string updatedData = string.Join(",", completedList);

            PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> {
                { "FindAround", updatedData }
                }
            },
            updateResult =>
            {
                Debug.Log("✅ Quiz berhasil disimpan sebagai selesai.");
                UpdateFindAroundButtonUI(true);
                CheckStatus();
            },
            error =>
            {
                Debug.LogError("❌ Gagal menyimpan progress kuis: " + error.GenerateErrorReport());
            });

        }, error =>
        {
            Debug.LogError("❌ Gagal ambil data user: " + error.GenerateErrorReport());
        });
    }

    public void SubmitScore(int score)
    {
        // CheckSession();
        // var statRequest = new UpdatePlayerStatisticsRequest
        // {
        //     Statistics = new List<StatisticUpdate>
        //     {
        //         new StatisticUpdate
        //         {
        //             StatisticName = leaderboardName,
        //             Value = score
        //         },
        //         new StatisticUpdate
        //         {
        //             StatisticName = leaderboardAllTIme,
        //             Value = score
        //         }
        //     }
        // };

        // PlayFabClientAPI.AddUserVirtualCurrency(new PlayFab.ClientModels.AddUserVirtualCurrencyRequest
        // {
        //     VirtualCurrency = "CO",
        //     Amount = score
        // },
        // result =>
        // {
        //     PlayFabClientAPI.UpdatePlayerStatistics(statRequest,
        //     result =>
        //         {
        //             Debug.Log("Skor berhasil dikirim ke PlayFab!");
        //         },
        //         error => Debug.LogError("Gagal kirim skor: " + error.GenerateErrorReport())
        //     );
        //     Debug.Log($"Berhasil menambahkan {score} koin. Total koin sekarang: {result.Balance}");
        // },
        // error => Debug.LogError("Gagal menambahkan koin: " + error.GenerateErrorReport()));
        Debug.Log($"🔼 Mengirim skor {score} ke PlayFab...");

        // 1. Kirim statistik dulu, jangan gabung dengan currency
        var statRequest = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
        {
            new StatisticUpdate { StatisticName = leaderboardName, Value = score },
            new StatisticUpdate { StatisticName = leaderboardAllTIme, Value = score }
        }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(statRequest,
            statResult =>
            {
                Debug.Log("✅ Statistik berhasil dikirim!");

                // 2. Baru tambahkan currency, tapi tidak wajib berhasil
                PlayFabClientAPI.AddUserVirtualCurrency(
                    new AddUserVirtualCurrencyRequest
                    {
                        VirtualCurrency = "CO",
                        Amount = score
                    },
                    currencyResult =>
                    {
                        Debug.Log($"💰 Berhasil menambahkan {score} CO. Total: {currencyResult.Balance}");
                    },
                    error =>
                    {
                        Debug.LogWarning("⚠ Currency gagal ditambahkan (tidak masalah): " + error.GenerateErrorReport());
                    }
                );
            },
            error =>
            {
                Debug.LogError("❌ Gagal kirim statistik: " + error.GenerateErrorReport());
            }
        );

    }

    public void AddUnlockBuilding(string newBuildingId)
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                string currentData = result.Data != null && result.Data.ContainsKey("unlockBuilding")
                    ? result.Data["unlockBuilding"].Value
                    : "";

                var buildingList = new List<string>(currentData.Split(','));

                if (!buildingList.Contains(newBuildingId))
                    buildingList.Add(newBuildingId);

                string updatedData = string.Join(",", buildingList);

                var updateRequest = new UpdateUserDataRequest
                {
                    Data = new Dictionary<string, string> { { "unlockBuilding", updatedData } }
                };

                PlayFabClientAPI.UpdateUserData(updateRequest,
                    updateResult =>
                    {
                        Debug.Log("Building data updated: " + updatedData);
                    },
                    error => Debug.LogError("Update failed: " + error.GenerateErrorReport()));
            },
            error => Debug.LogError("Get data failed: " + error.GenerateErrorReport()));
    }

}
