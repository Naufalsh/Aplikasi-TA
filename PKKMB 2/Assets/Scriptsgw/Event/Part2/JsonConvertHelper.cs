// using System.Collections.Generic;
// using UnityEngine;

// public static class JsonConvertHelper
// {
//     public static Dictionary<string, T> FromJson<T>(string json)
//     {
//         var rawDict = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
//         Dictionary<string, T> result = new();

//         foreach (var pair in rawDict)
//         {
//             string itemJson = MiniJSON.Json.Serialize(pair.Value);
//             result[pair.Key] = JsonUtility.FromJson<T>(itemJson);
//         }

//         return result;
//     }
// }