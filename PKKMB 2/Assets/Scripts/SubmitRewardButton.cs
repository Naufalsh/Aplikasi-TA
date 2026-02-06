using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

public class SubmitRewardButton : MonoBehaviour
{
    [Header("Reward Settings")]
    public int coinReward = 10; // Jumlah koin yang didapat

    [Header("UI Feedback")]
    public Button submitButton; // Tombol Submit itu sendiri

    private void Start()
    {
        // Jika lupa drag di inspector, cari otomatis
        if (submitButton == null) submitButton = GetComponent<Button>();

        // Pasang fungsi ke tombol saat diklik
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitClicked);
        }
    }

    void OnSubmitClicked()
    {
        // Matikan tombol biar ga diklik berkali-kali (spam)
        if (submitButton != null) submitButton.interactable = false;

        Debug.Log("🚀 Mengirim request reward koin...");

        // Panggil Fungsi Tambah Koin
        AddCoinReward();
    }

    void AddCoinReward()
    {
        var request = new AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = "CO", // Pastikan kode koin di PlayFab Anda adalah "CO"
            Amount = coinReward
        };

        PlayFabClientAPI.AddUserVirtualCurrency(request, 
            result => 
            {
                Debug.Log($"✅ SUKSES! Saldo Bertambah +{coinReward}. Total Koin: {result.Balance}");
                
                // Nyalakan tombol lagi (atau biarkan mati jika maunya sekali pakai)
                // if (submitButton != null) submitButton.interactable = true;
            }, 
            error => 
            {
                Debug.LogError("❌ GAGAL PlayFab: " + error.GenerateErrorReport());
                
                // Nyalakan tombol lagi biar user bisa coba ulang
                if (submitButton != null) submitButton.interactable = true;
            });
    }
}