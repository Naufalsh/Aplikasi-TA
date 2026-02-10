using UnityEngine;
using System.Collections;

public class infoPanelAnimation : MonoBehaviour
{  public RectTransform panel;

    private Vector2 startPos;   // posisi normal panel
    private Vector2 hiddenPos;  // posisi panel tersembunyi (bawah)

    void Start()
    {
        startPos = new Vector2(panel.anchoredPosition.x, -206);
        hiddenPos = new Vector2(panel.anchoredPosition.x, -1600);

        // pastikan panel berada di posisi start saat awal aktif
        panel.anchoredPosition = startPos;
    }

    public void OpenPanel()
    {
        // saat panel dihidupkan, langsung kembalikan ke posisi normal dulu
        panel.gameObject.SetActive(true);

        panel.anchoredPosition = hiddenPos; // spawn dari bawah

        LeanTween.move(panel, startPos, 0.35f).setEaseOutExpo();
    }

    public void ClosePanel()
    {
        LeanTween.move(panel, hiddenPos, 0.35f).setEaseInExpo()
            .setOnComplete(() => {
                panel.gameObject.SetActive(false);
                panel.anchoredPosition = startPos; // reset posisi biar aman
            });
    }
}