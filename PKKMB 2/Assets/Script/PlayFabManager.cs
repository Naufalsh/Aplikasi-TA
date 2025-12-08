using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayFabManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject personalInfoPanel;
    public GameObject waitingVerifPanel;
    public GameObject emailVerifAgainPanel;

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

    [Header("input Email Verification Again")]
    public TMP_InputField emailVeriftoSend;

    private string currentSessionId;
    private string tempEmail;
    private string tempPassword;
    private string _playFabId;
    private bool isRegist = false;
    private Coroutine _verifCheckCoroutine;
    

    private int[] groupIds = new int[] 
    {
        101001, 101002, 101003, 101004, 101005, 101006, 101007, 101008, 101009, 101010, 
        102001, 102002, 102003, 102004, 102005, 102006, 102007, 102008, 102009, 102010, 
        103001, 103002, 103003, 103004, 103005, 103006, 103007, 104001, 104002, 104003, 
        105001, 105002, 105003, 106001, 106002, 201001, 201002, 201003, 201004, 201005, 
        201006, 201007, 201008, 201009, 201010, 201011, 202001, 202002, 202003, 202004, 
        202005, 203001, 204001, 204002, 204003, 204004, 301001, 301002, 301003, 301004, 
        301005, 301006, 301007, 301008, 301009, 301010, 301011, 301012, 302001, 302002, 
        302003, 303001, 303002, 303003, 303004, 303005, 304001, 304002, 305001, 305002, 
        305003, 306001, 401001, 401002, 401003, 401004, 401005, 401006, 401007, 401008, 
        401009, 401010, 401011, 401012, 401013, 402001, 402002, 402003, 402004, 402005, 
        402006, 402007, 402008, 402009, 402010, 402011, 402012, 403001, 403002, 403003, 
        404001, 404002, 501001, 501002, 501003, 501004, 501005, 501006, 501007, 501008, 
        501009, 501010, 501011, 501012, 501013, 502001, 502002, 502003, 502004, 503001, 
        503002, 503003, 503004, 503005, 503006, 504001, 504002, 504003, 601001, 601002, 
        601003, 601004, 601005, 601006, 601007, 601008, 601009, 601010, 601011, 601012, 
        601013, 601014, 601015, 602001, 602002, 602003, 602004, 602005, 602006, 602007, 
        602008, 602009, 603001, 603002, 603003, 604001, 604002, 604003, 604004, 604005, 
        604006, 604007, 604008, 604009, 604010, 604011, 605001, 605002, 605003, 701001, 
        701002, 701003, 701004, 702001, 702002, 702003, 703001, 703002, 703003, 704001, 
        704002, 704003, 704004, 705001, 706001, 706002, 706003, 706004, 707001, 707002, 
        707003, 708001, 708002, 708003, 708004, 708005, 709001, 709002, 709003, 709004, 
        709005, 709006
    };

    [Header("Reset Password Fields")]
    public TMP_InputField resetEmailInput;

    void Start()
    {
        currentSessionId = SystemInfo.deviceUniqueIdentifier;
        ShowLoginPanel();
    }

    #region Panel Navigation
    public void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        personalInfoPanel.SetActive(false);
        waitingVerifPanel.SetActive(false);
        emailVerifAgainPanel.SetActive(false);
        ClearMessage();
    }

    public void ShowRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        personalInfoPanel.SetActive(false);
        waitingVerifPanel.SetActive(false);
        emailVerifAgainPanel.SetActive(false);
        ClearMessage();
    }

    public void ShowPersonalInfoPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        personalInfoPanel.SetActive(true);
        waitingVerifPanel.SetActive(false);
        emailVerifAgainPanel.SetActive(false);
        ClearMessage();
    }

    public void ShowWaitingVerifPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        personalInfoPanel.SetActive(false);
        waitingVerifPanel.SetActive(true);
        emailVerifAgainPanel.SetActive(false);
        ClearMessage();
    }

    public void ShowEmailVerifAgainPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        personalInfoPanel.SetActive(false);
        waitingVerifPanel.SetActive(false);
        emailVerifAgainPanel.SetActive(true);
        ClearMessage();
    }
    #endregion

    #region Button Functions
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

        tempEmail = registerEmailInput.text;
        tempPassword = registerPasswordInput.text;

        isRegist = true;
        ShowPersonalInfoPanel();
    }

    public void SignUpButton()
    {
        ClearMessage();
        var request = new RegisterPlayFabUserRequest
        {
            Email = tempEmail,
            Password = tempPassword,
            Username = usernameInput.text,
            DisplayName = usernameInput.text,
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
        var request = new SendAccountRecoveryEmailRequest
        {
            Email = resetEmailInput.text,
            TitleId = PlayFabSettings.TitleId
        };
        PlayFabClientAPI.SendAccountRecoveryEmail(request, OnPasswordReset, OnError);
    }

    public void ResendVerificationEmailButton()
    {
        if (!string.IsNullOrEmpty(emailVeriftoSend.text))
        {
            AddOrUpdateContactEmail(emailVeriftoSend.text);
        }
        else
        {
            Debug.LogError("Email tidak ditemukan. Silakan masukkan email.");
        }
    }
    #endregion

    #region PlayFab Callbacks
    void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        personalInfoMessageText.text = "Registrasi berhasil! Menyimpan data...";
        Debug.Log("Registration successful. Now updating user data.");

        _playFabId = result.PlayFabId;

        UpdateUserCustomData();

        GetPlayerProfileAndSetEmail(tempEmail);
    }

    void OnLoginSuccess(LoginResult result)
    {
        loginMessageText.text = "Logged In!";
        Debug.Log("Successful Login");
        _playFabId = result.PlayFabId;
        CreateSession();

        // Cek email verifikasi setelah login
        isRegist = false;
        GetPlayerProfileAndCheckEmailStatus();
    }

    void OnPasswordReset(SendAccountRecoveryEmailResult result)
    {
        loginMessageText.text = "Email untuk reset password telah dikirim.";
    }

    void OnDataUpdated(UpdateUserDataResult result)
    {
        Debug.Log("User data updated successfully.");
    }

    void OnError(PlayFabError error)
    {
        if (error.HttpCode == 409)
        {
            registerMessageText.text = "Email sudah digunakan. Gunakan email lain.";
            ShowRegisterPanel();
        }
        else
        {
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
                // { "Major", majorInput.text },
                // { "Faculty", facultyInput.text },
                { "GroupNumber", GetRandomGroup().ToString() }
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

    void GetPlayerProfileAndSetEmail(string emailAddress)
    {
        var request = new GetPlayerProfileRequest
        {
            PlayFabId = _playFabId,
            ProfileConstraints = new PlayerProfileViewConstraints
            {
                ShowContactEmailAddresses = true
            }
        };

        PlayFabClientAPI.GetPlayerProfile(request, result =>
        {
            if (result.PlayerProfile.ContactEmailAddresses != null &&
                result.PlayerProfile.ContactEmailAddresses.Count > 0)
            {
                Debug.Log("Akun sudah punya contact email, tidak perlu update.");
                // Jika ini dari flow register, lanjutkan ke verifikasi panel
                if (isRegist)
                {
                    ShowWaitingVerifPanel();
                    _verifCheckCoroutine = StartCoroutine(CheckEmailStatusRepeat(5));
                }
            }
            else
            {
                AddOrUpdateContactEmail(emailAddress);
            }
        }, OnError);
    }

    void AddOrUpdateContactEmail(string emailAddress)
    {
        var request = new AddOrUpdateContactEmailRequest
        {
            EmailAddress = emailAddress
        };
        PlayFabClientAPI.AddOrUpdateContactEmail(request, OnEmailUpdateSuccess, OnError);
    }

    void OnEmailUpdateSuccess(AddOrUpdateContactEmailResult result)
    {
        Debug.Log("Contact email berhasil ditambahkan. Email verifikasi akan dikirim.");
        ShowWaitingVerifPanel();

        // Mulai coroutine pengecekan berulang
        if (_verifCheckCoroutine != null) StopCoroutine(_verifCheckCoroutine);
        _verifCheckCoroutine = StartCoroutine(CheckEmailStatusRepeat(5));
    }

    private IEnumerator CheckEmailStatusRepeat(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            Debug.Log("Mulai memeriksa status verifikasi email...");

            GetPlayerProfileAndCheckEmailStatus();
        }
    }

    private void GetPlayerProfileAndCheckEmailStatus()
    {
        var request = new GetPlayerProfileRequest
        {
            PlayFabId = _playFabId,
            ProfileConstraints = new PlayerProfileViewConstraints
            {
                ShowContactEmailAddresses = true
            }
        };

        PlayFabClientAPI.GetPlayerProfile(request, OnGetPlayerProfileSuccess, OnError);
    }

    private void OnGetPlayerProfileSuccess(GetPlayerProfileResult result)
    {
        if (result.PlayerProfile.ContactEmailAddresses != null && result.PlayerProfile.ContactEmailAddresses.Count > 0)
        {
            var contactEmail = result.PlayerProfile.ContactEmailAddresses[0];

            if (contactEmail.VerificationStatus == EmailVerificationStatus.Confirmed)
            {
                Debug.Log("Email " + contactEmail.EmailAddress + " telah diverifikasi.");
                if (_verifCheckCoroutine != null) StopCoroutine(_verifCheckCoroutine);

                if (isRegist)
                {
                    SceneManager.LoadScene("Story Menu");
                }
                else
                {
                    SceneManager.LoadScene("Gameplay");
                }
            }
            else
            {
                Debug.Log("Email " + contactEmail.EmailAddress + " belum diverifikasi. Menunggu konfirmasi.");
                ShowWaitingVerifPanel();
            }
        }
        else
        {
            Debug.Log("Tidak ada email kontak yang ditemukan di profil pemain.");
            if (_verifCheckCoroutine != null) StopCoroutine(_verifCheckCoroutine);
            ShowEmailVerifAgainPanel();
        }
    }

    void ClearMessage()
    {
        if (loginMessageText != null) loginMessageText.text = "";
        if (registerMessageText != null) registerMessageText.text = "";
        if (personalInfoMessageText != null) personalInfoMessageText.text = "";
    }
    #endregion

    public int GetRandomGroup()
    {
        if (groupIds.Length > 0)
        {
            int randomIndex = Random.Range(0, groupIds.Length);
            
            // Langsung mengembalikan nilai (return)
            return groupIds[randomIndex];
        }
        else
        {
            Debug.LogError("Array kosong!");
            return -1; // Mengembalikan -1 jika terjadi error/array kosong
        }
    }

}