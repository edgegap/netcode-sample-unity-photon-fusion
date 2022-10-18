using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EdgegapConfig", menuName = "ScriptableObjects/EdgegapConfig", order = 1)]
public class EdgegapConfig : ScriptableObject
{
    public string AppName;
    public string Version;
    public string ApiToken;
    public string ApiEndpoint = "https://api.edgegap.com/v1/";
}
