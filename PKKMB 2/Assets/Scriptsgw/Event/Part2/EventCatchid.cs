// public string eventsRawJson;
// public bool eventsLoaded = false;

// public void LoadEvents()
// {
//     PlayFabClientAPI.GetTitleData(
//         new GetTitleDataRequest(),
//         result =>
//         {
//             if (result.Data.ContainsKey("Events"))
//             {
//                 eventsRawJson = result.Data["Events"];
//                 eventsLoaded = true;

//                 Debug.Log("Events loaded");
//             }
//         },
//         error =>
//         {
//             Debug.LogError(error.GenerateErrorReport());
//         });
// }

// public bool HasEventAtBuilding(string buildingId)
// {
//     if (!eventsLoaded) return false;

//     return eventsRawJson.Contains($"\"location\": \"{buildingId}\"");
// }