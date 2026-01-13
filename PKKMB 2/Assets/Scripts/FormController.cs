using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class FormManager : MonoBehaviour
{
    [Header("UI Controls")]
    public Toggle checkboxUmum;      // Checkbox "Daftar sebagai Umum"
    public Button signupButton;      // Tombol Sign Up

    [Header("Input Wajib (Semua User)")]
    // Masukkan Fullname dan Username di sini
    public List<TMP_InputField> commonInputs;

    [Header("Input Khusus Mahasiswa")]
    // Masukkan Major, Faculty, dan Group Number di sini
    public List<TMP_InputField> studentInputs;

    [Header("Panel Penutup (Opsional)")]
    // Masukkan 3 Panel abu-abu di sini
    public List<GameObject> disablePanels;

    private void Start()
    {
        // Setup awal checkbox
        if (checkboxUmum != null)
        {
            checkboxUmum.onValueChanged.AddListener(OnCheckboxChanged);
            // Jalankan sekali di awal untuk reset posisi
            OnCheckboxChanged(checkboxUmum.isOn);
        }

        // Matikan tombol di awal start jika form masih kosong
        CheckValidation(); 
    }

    private void Update()
    {
        // Cek terus menerus setiap frame apakah form sudah terisi
        CheckValidation();
    }

    // --- LOGIKA VISUAL (Hide/Show) ---
    public void OnCheckboxChanged(bool isVisitor)
    {
        // 1. Atur Input Field Mahasiswa
        foreach (TMP_InputField input in studentInputs)
        {
            if (input != null) 
            {
                // Jika Visitor (Umum), input dimatikan. Jika Mahasiswa, input dinyalakan.
                input.gameObject.SetActive(!isVisitor);
                
                // Opsional: Kosongkan teks jika dimatikan agar tidak mengganggu validasi
                if(isVisitor) input.text = ""; 
            }
        }

        // 2. Atur Panel Penutup (Visual Abu-abu)
        foreach (GameObject panel in disablePanels)
        {
            if (panel != null) panel.SetActive(isVisitor);
        }
    }

    // --- LOGIKA VALIDASI (Disable Button) ---
    private void CheckValidation()
    {
        bool isFormValid = true;

        // 1. Cek Common Inputs (Fullname, Username)
        // Harus terisi untuk SEMUA kondisi
        foreach (TMP_InputField input in commonInputs)
        {
            if (input == null || string.IsNullOrEmpty(input.text))
            {
                isFormValid = false;
                break; 
            }
        }

        // 2. Cek Student Inputs (Hanya jika BUKAN Visitor)
        // Jika Checkbox TIDAK dicentang, maka field ini wajib diisi
        if (checkboxUmum != null && !checkboxUmum.isOn)
        {
            foreach (TMP_InputField input in studentInputs)
            {
                if (input == null || string.IsNullOrEmpty(input.text))
                {
                    isFormValid = false;
                    break;
                }
            }
        }

        // 3. Terapkan ke Tombol
        if (signupButton != null)
        {
            // Jika isFormValid = false, tombol jadi abu-abu & tak bisa diklik
            // Jika isFormValid = true, tombol jadi warna normal & bisa diklik
            signupButton.interactable = isFormValid;
        }
    }
    
    // Fungsi dipanggil saat tombol diklik
    public void OnSignupClicked()
    {
        Debug.Log("Sign Up Berhasil! Data dikirim.");
    }
}