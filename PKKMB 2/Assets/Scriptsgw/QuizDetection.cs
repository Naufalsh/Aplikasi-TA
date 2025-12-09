using UnityEngine;
using UnityEngine.UI;

public class QuizDetection : MonoBehaviour
{
    public int[] quizId;              // isi ID quiz (1–41)
    public Button startQuizButton;  // drag tombol Start Quiz ke sini dari Inspector

    void Start()
    {
        CheckQuizStatus();
    }

    void CheckQuizStatus()
    {
        // cek apakah quiz sudah pernah dikerjakan
        if (PlayerPrefs.GetInt("quiz_done_" + quizId, 0) == 1)
        {
            Debug.Log("Quiz " + quizId + " sudah selesai, tombol dimatikan.");
            startQuizButton.interactable = false; // disable tombol
        }
        else
        {
            Debug.Log("Quiz " + quizId + " belum pernah dikerjakan.");
            startQuizButton.interactable = true; // tombol aktif
        }
    }

    // panggil ini setelah user menyelesaikan quiz
    public void MarkQuizAsDone()
    {
        PlayerPrefs.SetInt("quiz_done_" + quizId, 1);
        PlayerPrefs.Save();
        Debug.Log("Quiz " + quizId + " ditandai selesai!");

        // langsung matikan tombol biar user gak bisa klik lagi
        startQuizButton.interactable = false;
    }
}
