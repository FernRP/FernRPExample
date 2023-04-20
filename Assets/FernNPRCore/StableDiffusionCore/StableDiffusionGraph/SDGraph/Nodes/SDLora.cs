using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BlueGraph;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Networking;

namespace StableDiffusionGraph.SDGraph.Nodes
{
    [Node(Path = "SD Standard")]
    [Tags("SD Node")]
    public class SDLora : Node
    {
        
        [Input("Prompt")] public string prompt;
        [Input("Strength")] public float strength = 1;
        [Output("Lora")] public string lora;
        public string loraDir;
        public List<string> loraNames;
        public int currentIndex = 0;
        
        public SDLora()
        {
            EditorCoroutineUtility.StartCoroutine(ListLoraAsync(), this);
        }
        
        /// <summary>
        /// Get the list of available Stable Diffusion models.
        /// </summary>
        /// <returns></returns>
        IEnumerator ListLoraAsync()
        {
            // Stable diffusion API url for getting the models list
            string url = SDDataHandle.serverURL + SDDataHandle.DataDirAPI;

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
                SDDataDir m = JsonConvert.DeserializeObject<SDDataDir>(request.downloadHandler.text);
                // Keep only the names of the models
                loraDir = m.lora_dir;
                string[] files = Directory.GetFiles(loraDir, "*.safetensors", SearchOption.AllDirectories);
                if (loraNames == null) loraNames = new List<string>();
                loraNames.Clear();
                foreach (var f in files)
                {
                    loraNames.Add(Path.GetFileNameWithoutExtension(f));
                }
            }
            catch (Exception)
            {
                Debug.Log(request.downloadHandler.text);
                Debug.Log("Server needs and API key authentication. Please check your settings!");
            }
        }
        
        public override object OnRequestValue(Port port)
        {
            prompt = GetInputValue("Prompt", this.prompt);
            string result = $"{prompt},<lora:{lora}:{strength}>";
            return result;
        }
    }
}
