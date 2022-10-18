using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine.Networking;
using System.Threading;
using System.Collections;

public static class EdgegapAPIInterface
{
    private static EdgegapConfig Settings;
    private static bool initialized = false;
    private static string currentDeploymentRequestId;

    #region message objects

    struct DeploymentRequest
    {
        public struct EnvVar
        {
            public EnvVar(string key, string val)
            {
                this.key = key;
                this.value = val;
            }

            public string key;
            public string value;
        }

        public string app_name;
        public string version_name;
        public string[] ip_list;
        public EnvVar[] env_vars;

    }

    struct DeploymentResponse
    {
        public struct Location
        {
            public string city;
            public string country;
        }

        public struct PortMap
        {
            public int external;
            public int @internal;
            public string protocol;
        }

        public string request_id;
        public string request_dns;
        public string fqdn;
        public string public_ip;
        public string current_status;
        public bool running;
        public Location location;
        public Dictionary<string, PortMap> ports;

    }

    #endregion

    public static void Initialize(EdgegapConfig settings)
    {
        Settings = settings;
        initialized = true;
    }

    public static IEnumerator RequestDeployment(string[] ipAddresses, Dictionary<string, string> envVars)
    {
        if (!initialized)
        {
            Debug.LogError("[EdgegapInterface] Attempting to use EdgegapInterface before initialization");
            yield break;
        }

        currentDeploymentRequestId = null;

        DeploymentRequest reqData = new DeploymentRequest()
        {
            app_name = Settings.AppName,
            version_name = Settings.Version,
            ip_list = ipAddresses,
            env_vars = envVars.Select(nvp=> new DeploymentRequest.EnvVar(nvp.Key, nvp.Value)).ToArray()
        };

        string url = Settings.ApiEndpoint + "/v1/deploy";

        var reqDataString = JsonConvert.SerializeObject(reqData);
        var reqDataBytes = System.Text.Encoding.UTF8.GetBytes(reqDataString);

        using (UnityWebRequest req = UnityWebRequest.Post(url, ""))
        {
            req.uploadHandler = new UploadHandlerRaw(reqDataBytes);
            req.SetRequestHeader("Authorization", Settings.ApiToken);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(req.error);
                if(req.downloadHandler != null)
                {
                    Debug.LogError(req.downloadHandler.text);
                }
            }
            else
            {
                var response = JsonConvert.DeserializeObject<DeploymentResponse>(req.downloadHandler.text);
                currentDeploymentRequestId = response.request_id;
            }
        }
    }

    private static EdgegapServerInfoModel DeploymentResponseToInfoModel(DeploymentResponse respObj)
    {
        var serverInfo = new EdgegapServerInfoModel();

        if (!string.IsNullOrEmpty(respObj.public_ip))
        {
            serverInfo.IPAddress = respObj.public_ip;
        }
        else
        {
            serverInfo.IPAddress = Dns.GetHostAddresses(respObj.fqdn).Last().MapToIPv4().ToString();
        }
        serverInfo.Port = respObj.ports.First().Value.external;
        serverInfo.LocationDesc = respObj.location.city + ", " + respObj.location.country;
        return serverInfo;
    }

    public static IEnumerator WaitForDeployment()
    {
        for (int cntr = 0; cntr < 100; cntr++)
        {
            yield return new WaitForSeconds(1);
            using (var req = UnityWebRequest.Get(Settings.ApiEndpoint + "/v1/status/" + currentDeploymentRequestId))
            {
                req.SetRequestHeader("Authorization", Settings.ApiToken);
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(req.error);
                    break;
                }
                else
                {
                    var response = JsonConvert.DeserializeObject<DeploymentResponse>(req.downloadHandler.text);
                    if (response.current_status == "Status.READY")
                    {
                        //break and return if the server is ready
                        break;
                    }
                }
            }
        }
    }

    #region Server Methods

    public static IEnumerator StopDeploymentFromServer()
    {
        var deleteUrl = Environment.GetEnvironmentVariable("ARBITRIUM_DELETE_URL");
        var deleteToken = Environment.GetEnvironmentVariable("ARBITRIUM_DELETE_TOKEN");
        if (deleteUrl == null || deleteToken == null)
        {
            Debug.LogError("[EdgegapInterface] Unable to delete instance. Environment variables not set");
            yield break;
        }

        using (var req = UnityWebRequest.Delete(deleteUrl))
        {
            req.certificateHandler = new AllowAllCertificates();
            req.SetRequestHeader("Authorization", deleteToken);
            yield return req.SendWebRequest();

            if(req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(req.error);
            }
        }
    }

    public static IEnumerator GetPublicIpAndPortFromServer(Action<string, ushort> OnServerInfoRetrieved)
    {
        var contextUrl = Environment.GetEnvironmentVariable("ARBITRIUM_CONTEXT_URL");
        var contextToken = Environment.GetEnvironmentVariable("ARBITRIUM_CONTEXT_TOKEN");
        if(contextUrl == null || contextToken == null)
        {
            Debug.LogError("[EdgegapInterface] Missing context vars. Is this running on the edegegap network?");
            OnServerInfoRetrieved(null, 0);
            yield break;
        }


        using (var req = UnityWebRequest.Get(contextUrl))
        {
            req.SetRequestHeader("Authorization", contextToken);
            req.certificateHandler = new AllowAllCertificates();
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[EdgegapInterface] Error getting port\n"+req.error);
                OnServerInfoRetrieved(null, 0);
            }
            else
            {
                var response = JsonConvert.DeserializeObject<DeploymentResponse>(req.downloadHandler.text);
                var port = response.ports.First().Value.external;
                var publicIp = response.public_ip;
                yield return new WaitForSeconds(1); //give it a second to make sure port is open
                OnServerInfoRetrieved(publicIp, (ushort)port);
            }
        }
    }

    class AllowAllCertificates: CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
    #endregion
}
