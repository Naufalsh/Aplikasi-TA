using UnityEngine;

public class HighlightQuest : MonoBehaviour
{
    public Color highlightColor = Color.yellow;
    public float pulseSpeed;

    private Material questMat;
    private Color originalEmission;
    private bool isPlayerNear = false;

    void Start()
    {
        Renderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            questMat = renderer.material;
            // Pastikan material support Emission
            if (questMat.IsKeywordEnabled("_EMISSION"))
                originalEmission = questMat.GetColor("_EmissionColor");
            else
            {
                // Jika shader standar belum aktif emission-nya, enable dulu
                questMat.EnableKeyword("_EMISSION");
                originalEmission = Color.black;
            }
        }
    }

    void Update()
    {
        if (isPlayerNear && questMat != null)
        {
            float emissionStrength = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            Color finalColor = highlightColor * Mathf.LinearToGammaSpace(emissionStrength * 2f);
            questMat.SetColor("_EmissionColor", finalColor);
        }
    }

    // Fungsi ini dipanggil otomatis oleh Parent
    public void SetHighlight(bool status)
    {
        isPlayerNear = status;

        if (!status && questMat != null)
        {
            questMat.SetColor("_EmissionColor", originalEmission);
        }
    }
}