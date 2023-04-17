using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using BlueGraph;
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
        
        public SDCheckPoint()
        {
            EditorCoroutineUtility.StartCoroutine(ListModelsAsync(), this);
        }

        /// <summary>
        /// Get the list of available Stable Diffusion models.
        /// </summary>
        /// <returns></returns>
        IEnumerator ListModelsAsync()
        {
            // Stable diffusion API url for getting the models list
            string url = SDDataHandle.serverURL + SDDataHandle.ModelsAPI;

            UnityWebRequest request = new UnityWebRequest(url, "GET");
            request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
        
            if (SDDataHandle.UseAuth && !SDDataHandle.Username.Equals("") && !SDDataHandle.Password.Equals(""))
            {
                Debug.Log("Using API key to authenticate");
                byte[] bytesToEncode = Encoding.UTF8.GetBytes(SDDataHandle.Username + ":" + SDDataHandle.Password);
                string encodedCredentials = Convert.ToBase64String(bytesToEncode);
                request.SetRequestHeader("Authorization", "Basic " + encodedCredentials);
            }
        
            yield return request.SendWebRequest();

            try
            {
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
                Debug.Log(request.downloadHandler.text);
                Debug.Log("Server needs and API key authentication. Please check your settings!");
            }
        }

        public override object OnRequestValue(Port port)
        {
            return Model;
        }
    }
}