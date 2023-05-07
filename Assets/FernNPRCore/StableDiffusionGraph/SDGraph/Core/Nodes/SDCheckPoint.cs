using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using FernGraph;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

namespace StableDiffusionGraph.SDGraph.Nodes
{
    [Node(Path = "SD Standard")]
    [Tags("SD Node")]
    public class SDCheckPoint : Node
    {
        [Output] public string Model;

        public string[] modelNames;
        public int currentIndex = 0;

        public override void OnAddedToGraph()
        {
            base.OnAddedToGraph();
            EditorCoroutineUtility.StartCoroutine(ListModelsAsync(), this);
        }

        /// <summary>
        /// Get the list of available Stable Diffusion models.
        /// </summary>
        /// <returns></returns>
        IEnumerator ListModelsAsync()
        {
            // Stable diffusion API url for getting the models list
            string url = SDDataHandle.Instance.GetServerURL() + SDDataHandle.Instance.ModelsAPI;

            UnityWebRequest request = new UnityWebRequest(url, "GET");
            request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            if (SDDataHandle.Instance.GetUseAuth() && !string.IsNullOrEmpty(SDDataHandle.Instance.GetUserName()) && !string.IsNullOrEmpty(SDDataHandle.Instance.GetPassword()))
            {
                Debug.Log("Using API key to authenticate");
                byte[] bytesToEncode = Encoding.UTF8.GetBytes(SDDataHandle.Instance.GetUserName() + ":" + SDDataHandle.Instance.GetPassword());
                string encodedCredentials = Convert.ToBase64String(bytesToEncode);
                request.SetRequestHeader("Authorization", "Basic " + encodedCredentials);
            }
        
            yield return request.SendWebRequest();

            try
            {
                Debug.Log(request.downloadHandler.text);
                // Deserialize the response to a class
                SDModel[] ms = JsonConvert.DeserializeObject<SDModel[]>(request.downloadHandler.text);

                // Keep only the names of the models
                List<string> modelsNames = new List<string>();

                foreach (SDModel m in ms) 
                    modelsNames.Add(m.model_name);

                // Convert the list into an array and store it for futur use
                modelNames = modelsNames.ToArray();
            }
            catch (Exception)
            {
                Debug.Log("Server needs and API key authentication. Please check your settings!");
            }
        }

        public override object OnRequestValue(Port port)
        {
            return Model;
        }
    }
}