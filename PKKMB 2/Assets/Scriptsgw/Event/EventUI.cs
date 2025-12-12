using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class EventPanelUI : MonoBehaviour
{
    private TextMeshPro titleText;
    private TextMeshPro descriptionText;
    private Image eventImage;

    void Awake()
    {
        // Mencari SEMUA komponen TextMeshProUGUI di antara semua anak (termasuk yang non-aktif)
        TextMeshPro[] tmpros = GetComponentsInChildren<TextMeshPro>(true);
        bool foundTitle = false;
        bool foundDesc = false;

        // Iterasi melalui semua TMPro yang ditemukan dan cocokkan namanya
        foreach (TextMeshPro tmpro in tmpros)
        {
            Debug.Log("TMPro Ditemukan: " + tmpro.transform.name); // <--- DEBUG INI PENTING!

            if (tmpro.transform.name == "judulEvent")
            {
                titleText = tmpro;
                foundTitle = true;
            }
            else if (tmpro.transform.name == "descEvent")
            {
                descriptionText = tmpro;
                foundDesc = true;
            }
        }

        // Mencari SEMUA komponen Image di antara semua anak
        Image[] images = GetComponentsInChildren<Image>(true);
        bool foundImage = false;

        foreach (Image img in images)
        {
            // Kita harus hati-hati, karena mungkin ada banyak komponen Image (misalnya di Button)
            // Hanya ambil Image yang bernama "Image" (sesuai struktur Anda)
            if (img.transform.name == "Image")
            {
                eventImage = img;
                foundImage = true;
                Debug.Log("Image Ditemukan: " + img.transform.name);
                break; // Hentikan setelah ditemukan
            }
        }

        // Final Debug untuk memastikan
        if (!foundTitle) Debug.LogError("FINAL CHECK: TMPro 'judulEvent' TIDAK DITEMUKAN!");
        if (!foundDesc) Debug.LogError("FINAL CHECK: TMPro 'descEvent' TIDAK DITEMUKAN!");
        if (!foundImage) Debug.LogError("FINAL CHECK: Image 'Image' TIDAK DITEMUKAN!");
    }

public void SetData(EventDateu data)
    {
        // Pengecekan null ditambahkan untuk memastikan komponen sudah ditemukan
        if (titleText != null)
        {
            titleText.text = data.title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = data.description;
        }

        if (eventImage != null && !string.IsNullOrEmpty(data.image))
            StartCoroutine(LoadImage(data.image));
    }

    // Fungsi LoadImage sama seperti sebelumnya
    IEnumerator LoadImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = ((DownloadHandlerTexture)request.downloadHandler).texture;
            eventImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
