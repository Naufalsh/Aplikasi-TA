using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenLBScene : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OpenLB()
    {
        SceneManager.LoadScene("LeaderBoard");
    }

    public void OpenHistory()
    {
        SceneManager.LoadScene("UI History");
    }

    public void OpenGamePlay()
    {
        SceneManager.LoadScene("GamePlay");
    }

    public void OpenQuiz()
    {
        SceneManager.LoadScene("QuizChallenge");
    }

   public void OpenSearch()
    {
        SceneManager.LoadScene("GamePlaySearch");
    }


}
