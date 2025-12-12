using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;
using Mapbox.Unity.MeshGeneration.Factories;
using UnityEngine.EventSystems;

public class GoldenQuestTrigger : MonoBehaviour
{
    // public static GoldenQuestTrigger Instance;   // bisa dipanggil dari luar

    // private void Awake()
    // {
    //     if (Instance == null)
    //     {
    //         Instance = this;
    //         // Kalau mau objek ini tidak hancur antar scene
    //         // DontDestroyOnLoad(gameObject);
    //     }
    //     else if (Instance != this)
    //     {
    //         Destroy(gameObject); // jaga supaya hanya ada 1 instance
    //     }
    // }
    [SerializeField] public string buildingId; // diisi otomatis dari SpawnOnMap
    private bool playerInZone = false;

    // Panel Info
    private GameObject infoPanel;
    private TextMeshProUGUI judulQuest;
    private TextMeshProUGUI deskripsiQuest;
    private Image imageQuest;

    // Tambahan: kalau nanti ada mini quiz
    private TextMeshProUGUI namaGedung2;
    private TextMeshProUGUI listAround, listAround2, listAround3;
    private Toggle toggle1, toggle2, toggle3;
    private Button submitButton;
    private TextMeshProUGUI textSubmitButton;
    private string currentSessionId;
    private string leaderboardName = "Leaderboard";
    private string leaderboardAllTIme = "Leaderboard_AllTime";
    public TextMeshProUGUI StartText;
    public Button ButtonStartQuiz;

    private string findTheBuildingTemplate =
        "Look around {BUILDING_NAME}, check the box below that you think is correct. Submit your answer to earn bonus points!";
    private HighlightQuest highlightScript;

    DirectionsFactory directionsFactory;

    void Awake()
    {
        directionsFactory = FindObjectOfType<DirectionsFactory>();
    }
    void Start()
    {
        highlightScript = GetComponentInChildren<HighlightQuest>();

        if (highlightScript == null)
        {
            Debug.LogError($"❌ Error di {gameObject.name}: Script HighlightQuest tidak ditemukan di anak-anaknya!");
        }

        currentSessionId = SystemInfo.deviceUniqueIdentifier;
        // CheckStatus();
        // Cari panel Info sama seperti QuestionMark
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject panel in allObjects)
        {
            if (panel.name == "Info")
            {
                infoPanel = panel;
                judulQuest = infoPanel.transform.Find("JudulGedung").GetComponent<TextMeshProUGUI>();
                deskripsiQuest = infoPanel.transform.Find("Scroll View/Viewport/Content/InfoGedung").GetComponent<TextMeshProUGUI>();
                imageQuest = infoPanel.transform.Find("ImageGedung").GetComponent<Image>();

                ButtonStartQuiz = infoPanel.transform.Find("QuizSection/ButtonStartQuiz").GetComponent<Button>();
                StartText = infoPanel.transform.Find("QuizSection/ButtonStartQuiz/StartText").GetComponent<TextMeshProUGUI>();


                // Jika quest juga butuh toggle quiz
                namaGedung2 = infoPanel.transform.Find("FindTheBuildingSection/Deskripsi Quiz")?.GetComponent<TextMeshProUGUI>();
                listAround = infoPanel.transform.Find("FindTheBuildingSection/Toggle/Label")?.GetComponent<TextMeshProUGUI>();
                listAround2 = infoPanel.transform.Find("FindTheBuildingSection/Toggle2/Label")?.GetComponent<TextMeshProUGUI>();
                listAround3 = infoPanel.transform.Find("FindTheBuildingSection/Toggle3/Label")?.GetComponent<TextMeshProUGUI>();

                toggle1 = infoPanel.transform.Find("FindTheBuildingSection/Toggle")?.GetComponent<Toggle>();
                toggle2 = infoPanel.transform.Find("FindTheBuildingSection/Toggle2")?.GetComponent<Toggle>();
                toggle3 = infoPanel.transform.Find("FindTheBuildingSection/Toggle3")?.GetComponent<Toggle>();
                submitButton = infoPanel.transform.Find("FindTheBuildingSection/SubmitButton")?.GetComponent<Button>();
                textSubmitButton = infoPanel.transform.Find("FindTheBuildingSection/SubmitButton/TextSubmit")?.GetComponent<TextMeshProUGUI>();
            }
        }



        if (infoPanel != null)
            infoPanel.SetActive(false);


    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            highlightScript.SetHighlight(true);
            playerInZone = true;
            Debug.Log("Player masuk ke zona GoldenQuest: " + buildingId);
            CheckStatus();
            if (QuestionMarkManager.Instance != null)
            {
                QuestionMarkManager.Instance.currentBuildingId = buildingId;
                // CheckQuizStatus(); //ini bikin error
            }
            if (buildingId == directionsFactory.targetId)
            {
                directionsFactory.ClearRoute();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            highlightScript.SetHighlight(false);
            playerInZone = false;
            Debug.Log("Player keluar dari zona GoldenQuest: " + buildingId);
        }
    }

    private void OnMouseUpAsButton()
    {
        if (IsPointerOverUI())
        {
            Debug.Log("Gabisa mencet ahihhi");
            return;
        }

        if (!playerInZone) return; // hanya bisa diklik kalau player ada di zona

        if (GameManager.Instance != null && GameManager.Instance.buildingCache.ContainsKey(buildingId))
        {
            BuildingData target = GameManager.Instance.buildingCache[buildingId];

            // Set isi panel
            judulQuest.text = target.name;
            deskripsiQuest.text = target.description;
            namaGedung2.text = findTheBuildingTemplate.Replace("{BUILDING_NAME}", target.name);

            SetQuestImage(target.id);

            // ButtonStartQuiz.onClick.RemoveAllListeners();
            // ButtonStartQuiz.onClick.AddListener(CheckStatus);
            // CheckStatus();
            // (Opsional) isi toggle dari FindAroundBuilding.Instance jika ada data
            FindAroundSet findAroundSet = FindAroundBuilding.Instance.GetFindAroundByBuilding(buildingId);
            if (findAroundSet != null && findAroundSet.quests.Count > 0)
            {
                var quest = findAroundSet.quests[0];
                if (quest.options.Count > 0) listAround.text = quest.options[0];
                if (quest.options.Count > 1) listAround2.text = quest.options[1];
                if (quest.options.Count > 2) listAround3.text = quest.options[2];
            }

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(SubmitAnswer);
            }

            infoPanel.SetActive(true);
            Debug.Log($"GoldenQuest {buildingId} ditekan -> tampilkan panel info");
        }
        else
        {
            Debug.LogWarning("⚠ Data GoldenQuest " + buildingId + " tidak ditemukan di GameManager!");
        }
    }

    private bool IsPointerOverUI()
    {
        // Cek Mouse (Editor)
        if (EventSystem.current.IsPointerOverGameObject()) return true;

        // Cek Touch (HP)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return true;
        }

        return false;
    }

    private void SetQuestImage(string questId)
    {
        Sprite sprite = Resources.Load<Sprite>("BuildingInfoImage/" + questId);
        if (sprite != null)
        {
            imageQuest.sprite = sprite;
        }
        else
        {
            Debug.LogWarning("⚠ Gambar untuk GoldenQuest " + questId + " tidak ditemukan!");
        }
    }

    // --- Tambahan: kalau quest ini juga butuh evaluasi jawaban ---
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
        Debug.Log("Ini Id golden quest ke sekian - " + buildingId);
        if (findAroundSet == null || findAroundSet.quests.Count == 0) return;

        var quest = findAroundSet.quests[0];
        List<string> selectedAnswers = new List<string>();

        if (toggle1 != null && toggle1.isOn) selectedAnswers.Add(listAround.text);
        if (toggle2 != null && toggle2.isOn) selectedAnswers.Add(listAround2.text);
        if (toggle3 != null && toggle3.isOn) selectedAnswers.Add(listAround3.text);

        List<string> correctAnswers = quest.answer;
        int correctSelected = selectedAnswers.FindAll(a => correctAnswers.Contains(a)).Count;
        int incorrectSelected = selectedAnswers.FindAll(a => !correctAnswers.Contains(a)).Count;
        int totalCorrectAnswersAvailable = correctAnswers.Count;

        int finalScore = 0;
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
            }
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
                AddUnlockBuilding(buildingId);
                DisableMe();
            }

        },
        error =>
        {
            Debug.LogError("❌ Gagal memeriksa status kuis: " + error.GenerateErrorReport());
        });
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
    void DisableMe()
    {
        // Menonaktifkan GameObject tempat script ini menempel
        gameObject.SetActive(false);
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
