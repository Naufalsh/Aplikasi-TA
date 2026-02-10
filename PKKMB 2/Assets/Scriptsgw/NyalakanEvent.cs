using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NyalakanEvent : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject targetPanel;
    public float displayTime = 1f;
    void Start()
    {


    }

    // Update is called once per frame
    void Update()
    {

    }
    public void TampilkanPanelSesaat()
    {
        StartCoroutine(ProsesPanel());
    }

    private IEnumerator ProsesPanel()
    {
        // 1. Aktifkan Panel
        targetPanel.SetActive(true);
        Debug.Log("Panel Aktif");

        // 2. Tunggu selama X detik
        yield return new WaitForSeconds(displayTime);

        // 3. Matikan Panel
        targetPanel.SetActive(false);
        Debug.Log("Panel Mati");
    }
}
