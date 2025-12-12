using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class PixelPerfectText : MonoBehaviour
{

    // public int upscaleFactor = 3;
    // private Text txt;

    // void Awake()
    // {
    //     txt = GetComponent<Text>();

    //     // Perbesar font, tapi JANGAN perkecil scale
    //     txt.fontSize = txt.fontSize * upscaleFactor;
    //     txt.resizeTextMaxSize = txt.fontSize;
    //     txt.resizeTextForBestFit = false;

    //     // Tapi kecilkan rect SIZE agar kembali ke ukuran visual semula
    //     var rect = GetComponent<RectTransform>();
    //     rect.sizeDelta /= upscaleFactor;
    // }
        public int upscaleFactor = 2;   // Semakin besar, semakin tajam
        private Text txt;
        private RectTransform rect;

        private int originalFontSize;
        private Vector3 originalScale;

        void Awake()
        {
            txt = GetComponent<Text>();
            rect = GetComponent<RectTransform>();

            originalFontSize = txt.fontSize;
            originalScale = rect.localScale;

            ApplySuperSampling();
        }

        void ApplySuperSampling()
        {
            // Naikkan font size internal
            txt.fontSize = originalFontSize * upscaleFactor;

            // Kecilkan scale UI-nya
            rect.localScale = originalScale / upscaleFactor;

            // Pastikan teks diupdate ulang
            Canvas.ForceUpdateCanvases();
        }
}
