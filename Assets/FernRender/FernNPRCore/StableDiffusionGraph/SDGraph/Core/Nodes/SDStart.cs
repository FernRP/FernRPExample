using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FernGraph;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FernNPRCore.StableDiffusionGraph
{
    [Node(Path = "Events")]
    [Tags("SD Events")]
    [Output("SDFlowOut", typeof(SDFlowData), Multiple = false)]
    public class SDStart : Node, ICanExecuteSDFlow
    {
        [Input("Model", Editable = false)] public string Model;
        public string serverURL = "http://127.0.0.1:7860";
        public bool overrideSettings;
        public bool useAuth = false;
        public string user = "";
        public string pass = "";

        public override object OnRequestValue(Port port) => null;

        public IEnumerator Execute()
        {
            Model = GetInputValue("Model", this.Model);
            SDUtil.SDLog($"Use {Model}");
            yield return SetModelAsync(Model);
        }

        public virtual ICanExecuteSDFlow GetNext()
        {
            var port = GetPort("SDFlowOut");
            return port.ConnectedPorts.FirstOrDefault()?.Node as ICanExecuteSDFlow;
        }

        // <summary>
        /// Set a model to use by Stable Diffusion.
        /// </summary>
        /// <param name="modelName">Model to set</param>
        /// <returns></returns>
        public IEnumerator SetModelAsync(string modelName)
        {
            if (overrideSettings&&!string.IsNullOrEmpty(serverURL))
            {
                SDDataHandle.Instance.OverrideSettings = true;
                SDDataHandle.Instance.OverrideServerURL = serverURL;
                SDDataHandle.Instance.OverrideUseAuth = useAuth;
                SDDataHandle.Instance.OverrideUsername = user;
                SDDataHandle.Instance.OverridePassword = pass;
            }
            else
            {
                SDDataHandle.Instance.OverrideSettings = false;
            }
            // Stable diffusion API url for setting a model
            string url = SDDataHandle.Instance.GetServerURL()+SDDataHandle.Instance.OptionAPI;

            // Load the list of models if not filled already
            if (string.IsNullOrEmpty(Model))
            {
                Debug.Log("Model is null");
                yield return null;
            }

            try
            {
                // Tell Stable Diffusion to use the specified model using an HTTP POST request
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                if (SDDataHandle.Instance.GetUseAuth() && !string.IsNullOrEmpty(SDDataHandle.Instance.GetUserName()) && !string.IsNullOrEmpty(SDDataHandle.Instance.GetPassword()))
                {
                    httpWebRequest.PreAuthenticate = true;
                    byte[] bytesToEncode = Encoding.UTF8.GetBytes(SDDataHandle.Instance.GetUserName() + ":" + SDDataHandle.Instance.GetPassword());
                    string encodedCredentials = Convert.ToBase64String(bytesToEncode);
                    httpWebRequest.Headers.Add("Authorization", "Basic " + encodedCredentials);
                }

                // Write to the stream the JSON parameters to set a model
                {
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        // Model to use
                        SDOption sd = new SDOption();
                        sd.sd_model_checkpoint = modelName;

                        // Serialize into a JSON string
                        string json = JsonConvert.SerializeObject(sd);

                        // Send the POST request to the server
                        streamWriter.Write(json);
                    }

                    // Get the response of the server
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        string result = streamReader.ReadToEnd();
                        // We actually don't care about the response, we are not expecting anything particular
                        // We do this only to make sure we don't return from this function until the server has given a response (processed the request)
                    }
                }
            }
            catch (WebException e)
            {
                Debug.Log("Error: " + e.Message);
            }
        }

        public override void OnValidate()
        {
            base.OnValidate();
            var stableGraph = this.Graph as StableDiffusionGraph;
            stableGraph.serverURL = SDDataHandle.Instance.GetServerURL();
        }
    }
}
