using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EdgegapManager : MonoBehaviour
{
    public static bool EdgegapPreServerMode { get; set; }

    public static bool TransferingToEdgegapServer { get; set; }

    public static string EdgegapRoomCode { get; set; }

    public static int ExpectedPlayerCount { get; set; } 

    public static bool IsServer()
    {
        bool serverDebugging = false;
#if UNITY_EDITOR
      //  serverDebugging = ParrelSync.ClonesManager.IsClone();
#endif

        return Application.isBatchMode || serverDebugging;
    }

   
    public EdgegapConfig config;

    public static EdgegapManager Instance;

    private void Awake()
    {
        if(Instance && Instance != this)
        {
            Destroy(this);
        }

        Instance = this;
        if (string.IsNullOrEmpty(this.config.ApiToken))
        {
            Debug.LogError("Missing API token in EdgegapConfig file.  Did you forget to add it?");
            StopEverything();
        }
        else if (string.IsNullOrEmpty(this.config.Version))
        {
            Debug.LogError("Missing app version in EdgegapConfig file.  Did you forget to add it?");
            StopEverything();
        }

        EdgegapAPIInterface.Initialize(this.config);

        if (IsServer())
        {
            EdgegapRoomCode = Environment.GetEnvironmentVariable("room_code") ?? "test";
            ExpectedPlayerCount = int.Parse(Environment.GetEnvironmentVariable("player_count") ?? "1");
            Debug.Log($"Starting server room at with code {EdgegapRoomCode} and expecting {ExpectedPlayerCount} players");
        }
    }

    void StopEverything()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPaused = true;
#endif
    }

    /// <summary>
    /// Calls the Edgegap API to deploy a server
    /// </summary>
    /// <param name="clientIpList">A list of client IP addresses</param>
    /// <param name="OnDeployed">A callback method with the session name as a parameter</param>
    public IEnumerator Deploy(string[] clientIpList, Action<string> OnDeployed = null)
    {
        var roomCode = Guid.NewGuid().ToString();
        yield return EdgegapAPIInterface.RequestDeployment(clientIpList,
             new Dictionary<string, string> {
                { "room_code", roomCode },
                { "player_count", clientIpList.Length.ToString() } });

        yield return EdgegapAPIInterface.WaitForDeployment();

        OnDeployed(roomCode);
    }

    public IEnumerator GetPublicIpAddress(Action<string> OnRetrieved)
    {
        using(var req = UnityWebRequest.Get("https://api.ipify.org/"))
        {
            yield return req.SendWebRequest();
            OnRetrieved(req.downloadHandler.text);
        }
    }
}
