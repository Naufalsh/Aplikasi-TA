using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.SceneManagement;

public class RegisTest : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject personalInfoPanel;

    [Header("Login Fields")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;

    [Header("Register Fields")]
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField confirmPasswordInput;

    [Header("Personal Info Fields")]
    public TMP_InputField usernameInput;
    public TMP_InputField fullnameInput;
    public TMP_InputField majorInput;
    public TMP_InputField facultyInput;
    public TMP_InputField groupNumberInput;

    [Header("Message Display")]
    public TMP_Text loginMessageText;
    public TMP_Text registerMessageText;
    public TMP_Text personalInfoMessageText;

    private string currentSessionId;
    private string tempEmail;
    private string tempPassword;

    void Start()
    {
        currentSessionId = SystemInfo.deviceUniqueIdentifier;
        // Tampilkan panel login secara default saat mulai
        ShowLoginPanel();
    }

    #region Panel Navigation
    public void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        personalInfoPanel.SetActive(false);
        ClearMessage();
    }

    public void ShowRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        personalInfoPanel.SetActive(false);
        ClearMessage();
    }
    #endregion

    #region Button Functions
    // Tombol di panel register awal, untuk lanjut ke panel info pribadi
    public void ProceedToPersonalInfoButton()
    {
        ClearMessage();
        if (registerPasswordInput.text.Length < 6)
        {
            registerMessageText.text = "Password minimal 6 karakter.";
            return;
        }
        if (registerPasswordInput.text != confirmPasswordInput.text)
        {
            registerMessageText.text = "Password dan konfirmasi tidak cocok.";
            return;
        }

        // Simpan email dan password sementara
        tempEmail = registerEmailInput.text;
        tempPassword = registerPasswordInput.text;

        // Pindah ke panel berikutnya
        registerPanel.SetActive(false);
        personalInfoPanel.SetActive(true);
    }

    // Tombol "Sign Up" final di panel info pribadi
    public void SignUpButton()
    {
        ClearMessage();
        var request = new RegisterPlayFabUserRequest
        {
            Email = tempEmail, // Gunakan email yang disimpan
            Password = tempPassword, // Gunakan password yang disimpan
            Username = usernameInput.text,
            RequireBothUsernameAndEmail = false
        };
        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnError);
    }

    public void LoginButton()
    {
        ClearMessage();
        var request = new LoginWithEmailAddressRequest
        {
            Email = loginEmailInput.text,
            Password = loginPasswordInput.text,
        };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnError);
    }

    public void ResetPasswordButton()
    {
        ClearMessage();
        // Asumsi email untuk reset password diambil dari form login
        var request = new SendAccountRecoveryEmailRequest
        {
            Email = loginEmailInput.text,
            TitleId = "438B3" // Ganti dengan Title ID Anda
        };
        PlayFabClientAPI.SendAccountRecoveryEmail(request, OnPasswordReset, OnError);
    }
    #endregion

    #region PlayFab Callbacks
    void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        personalInfoMessageText.text = "Registrasi berhasil! Menyimpan data...";
        Debug.Log("Registration successful. Now updating user data.");
        
        // Setelah registrasi sukses, simpan data tambahan dari form info pribadi
        UpdateUserCustomData();
    }

    void OnLoginSuccess(LoginResult result)
    {
        // Pesan sukses login ditampilkan di panel login
        loginMessageText.text = "Logged In!";
        Debug.Log("Successful Login");
        CreateSession();
        SceneManager.LoadScene("GamePlay");
    }

    void OnPasswordReset(SendAccountRecoveryEmailResult result)
    {
        loginMessageText.text = "Email untuk reset password telah dikirim.";
    }

    void OnDataUpdated(UpdateUserDataResult result)
    {
        Debug.Log("User data updated successfully.");
        // Setelah semua data disimpan, selesaikan proses login
        // OnLoginSuccess(null); // Panggil OnLoginSuccess untuk pindah scene
        SceneManager.LoadScene("Story Menu");
    }

    void OnError(PlayFabError error)
    {
        // Menampilkan pesan error di panel yang relevan
        if (loginPanel.activeSelf)
        {
            loginMessageText.text = error.ErrorMessage;
        }
        else if (registerPanel.activeSelf)
        {
            registerMessageText.text = error.ErrorMessage;
        }
        else if (personalInfoPanel.activeSelf)
        {
            personalInfoMessageText.text = error.ErrorMessage;
        }
        Debug.LogError(error.GenerateErrorReport());
    }
    #endregion

    #region Helper Functions
    void UpdateUserCustomData()
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "Username", usernameInput.text }, 
                { "Fullname", fullnameInput.text },
                { "Major", majorInput.text },
                { "Faculty", facultyInput.text },
                { "GroupNumber", groupNumberInput.text }
            }
        };
        PlayFabClientAPI.UpdateUserData(request, OnDataUpdated, OnError);
    }

    void CreateSession()
    {
        var updateRequest = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "deviceSession", currentSessionId }
            }
        };

        PlayFabClientAPI.UpdateUserData(updateRequest,
            updateResult => Debug.Log("Session saved."),
            error => Debug.LogError("Failed to save session: " + error.GenerateErrorReport()));
    }

    void ClearMessage()
    {
        if (loginMessageText != null)
        {
            loginMessageText.text = "";
        }
        if (registerMessageText != null)
        {
            registerMessageText.text = "";
        }
        if (personalInfoMessageText != null)
        {
            personalInfoMessageText.text = "";
        }
    }
    #endregion
}