using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;
using System;

public class ChangeAvatar : MonoBehaviour
{
    private string currentSessionId;
    public Transform content;

    public static Action<Transform> OnCharacterSpawned;

    // Tambahkan variabel untuk menyimpan instance karakter
    private GameObject currentCharacterInstance;

    void Start()
    {
        currentSessionId = SystemInfo.deviceUniqueIdentifier;
        getCurrentChar();
    }

    public void getCurrentChar()
    {
        CheckSession();
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data != null && result.Data.ContainsKey("currentChar"))
            {
                string idChar = result.Data["currentChar"].Value;
                Debug.Log("Dapat ID karakter dari UserData: " + idChar);

                Transform[] allChildren = content.GetComponentsInChildren<Transform>();

                foreach (Transform child in content)
                {
                    if (child.name != idChar) 
                    {
                        child.gameObject.SetActive(false); 
                    }
                    else
                    {
                        child.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                Debug.LogWarning("UserData tidak mengandung key 'currentChar'");
            }
        },
    error =>
    {
        Debug.LogError("Gagal ambil UserData: " + error.GenerateErrorReport());
    });
    }

    public void changeCharacter(string idChar)
    {
        CheckSession();
        var updateUserDataRequest = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
                        {
                            { "currentChar", idChar}
                        },
                        Permission = UserDataPermission.Public
        };

        PlayFabClientAPI.UpdateUserData(updateUserDataRequest,
            updateResult =>
            {
                Debug.Log($"Character diperbarui ke: {idChar}");
                getCurrentChar();
            }, error => Debug.LogError("Gagal update character: " + error.GenerateErrorReport()));
    }

    void CheckSession()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data != null && result.Data.ContainsKey("deviceSession"))
            {
                string sessionFromServer = result.Data["deviceSession"].Value;

                if (sessionFromServer != currentSessionId)
                {
                    Debug.LogWarning("Session tidak valid. User login dari device lain.");
                    SceneManager.LoadScene("Main Menu");
                }
            }
        },
        error => Debug.LogError("Gagal ambil session: " + error.GenerateErrorReport()));
    }
}