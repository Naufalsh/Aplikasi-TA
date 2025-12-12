using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToggleOptionUI : MonoBehaviour
{
    public RawImage background;
    public TextMeshProUGUI label;
    public RawImage frame;

    public Color normalBg = new Color(0.95f, 0.95f, 0.95f);   // abu muda
    public Color selectedBg = new Color(1f, 0.9f, 0.9f);      // merah muda

    public Color normalText = new Color(0.2f, 0.2f, 0.2f);    // hitam keabu
    public Color selectedText = new Color(1f, 0f, 0f);        // merah

    private Toggle toggle;

    void Start()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleChanged);

        // Set awal
        OnToggleChanged(toggle.isOn);
    }

    void OnToggleChanged(bool isOn)
    {
        background.color = isOn ? selectedBg : normalBg;
        label.color = isOn ? selectedText : normalText;
        if (frame != null)
            frame.gameObject.SetActive(isOn);
    }
}
