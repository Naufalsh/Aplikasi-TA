using UnityEngine;
using UnityEngine.SceneManagement;

public class EventMarkClick : MonoBehaviour
{
    public string buildingId;

    private void OnMouseUpAsButton()
    {
        EventDataQuiz ev = GameManager.Instance.GetEventByLocation(buildingId);

        if (ev == null)
        {
            Debug.LogWarning("Event tidak ditemukan");
            return;
        }

        GameManager.Instance.currentSelectedEvent = ev;
        GameManager.Instance.currentEventBuildingId = buildingId;

        SceneManager.LoadScene("QuizChallenge 1");
    }
}