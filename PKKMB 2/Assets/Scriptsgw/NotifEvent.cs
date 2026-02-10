using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class NotifEvent : MonoBehaviour
{
    public GameObject notif;

    void Start()
    {
        notif.SetActive(false); // default mati
        GetEventFromPlayFab();
    }

    void GetEventFromPlayFab()
    {
        PlayFabClientAPI.GetTitleData(
            new GetTitleDataRequest(),
            OnGetTitleDataSuccess,
            OnGetTitleDataError
        );
    }

    void OnGetTitleDataSuccess(GetTitleDataResult result)
    {
        // cek Data ada
        if (result.Data == null)
        {
            notif.SetActive(false);
            Debug.Log("TitleData NULL");
            return;
        }

        // cek key "Events" ada
        if (!result.Data.ContainsKey("Events"))
        {
            notif.SetActive(false);
            Debug.Log("Key Events TIDAK ADA");
            return;
        }

        // cek isi Events tidak kosong
        string eventsData = result.Data["Events"];

        if (!string.IsNullOrEmpty(eventsData))
        {
            Debug.Log("Events ADA & TIDAK KOSONG");
            notif.SetActive(true);
        }
        else
        {
            Debug.Log("Events ADA tapi KOSONG");
            notif.SetActive(false);
        }
    }

    void OnGetTitleDataError(PlayFabError error)
    {
        Debug.LogError(error.GenerateErrorReport());
        notif.SetActive(false);
    }
}
