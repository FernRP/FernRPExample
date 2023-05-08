//#define GETLOARMODELS TODO: Wait for lora's api to pass
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FernGraph;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Networking;

namespace FernNPRCore.StableDiffusionGraph
{
    [Node(Path = "SD Standard")]
    [Tags("SD Node")]
    public class SDLora : Node
    {
        
        [Input("Prompt")] public string prompt;
        [Input("LoRAPrompt")] public string loraPrompt = "";
        [Input("Strength")] public float strength = 1;
        [Output("Lora")] public string lora;
        public string loraDir;
        public List<string> loraNames;
        public int currentIndex = 0;

        public StableDiffusionGraph stableGraph;
        
        public override void OnAddedToGraph()
        {
            base.OnAddedToGraph();
            stableGraph = Graph as StableDiffusionGraph;
            EditorCoroutineUtility.StartCoroutine(ListLoraAsync(), this);
        }

        /// <summary>
        /// Get the list of available Stable Diffusion models.
        /// </summary>
        /// <returns></returns>
        public IEnumerator ListLoraAsync()
        {
#if GETLOARMODELS
            if (loraNames == null)
                loraNames = new List<string>();
            else
                loraNames.Clear();
            // Stable diffusion API url for getting the models list
            string url = SDDataHandle.Instance.GetServerURL() + SDDataHandle.Instance.LorasAPI;
            SDUtil.Log(url);

            UnityWebRequest request = new UnityWebRequest(url, "GET");
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            if (SDDataHandle.Instance.GetUseAuth() && !string.IsNullOrEmpty(SDDataHandle.Instance.GetUserName()) && !string.IsNullOrEmpty(SDDataHandle.Instance.GetPassword()))
            {
                SDUtil.Log("Using API key to authenticate");
                byte[] bytesToEncode = Encoding.UTF8.GetBytes(SDDataHandle.Instance.GetUserName() + ":" + SDDataHandle.Instance.GetPassword());
                string encodedCredentials = Convert.ToBase64String(bytesToEncode);
                request.SetRequestHeader("Authorization", "Basic " + encodedCredentials);
            }

            yield return request.SendWebRequest();

            try
            {
                SDUtil.Log(request.downloadHandler.text);
                // Deserialize the response to a class
                SDLoraModel[] ms = JsonConvert.DeserializeObject<SDLoraModel[]>(request.downloadHandler.text);

                foreach (var m in ms)
                    loraNames.Add(m.name);
            }
            catch (Exception)
            {
                SDUtil.LogError("Server needs and API key authentication. Please check your settings!");
            }
#else
            // Stable diffusion API url for getting the models list
            string url = SDDataHandle.Instance.GetServerURL() + SDDataHandle.Instance.DataDirAPI;

            UnityWebRequest request = new UnityWebRequest(url, "GET");
            request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
        
            if (SDDataHandle.Instance.UseAuth && !SDDataHandle.Instance.Username.Equals("") && !SDDataHandle.Instance.Password.Equals(""))
            {
                SDUtil.Log("Using API key to authenticate");
                byte[] bytesToEncode = Encoding.UTF8.GetBytes(SDDataHandle.Instance.Username + ":" + SDDataHandle.Instance.Password);
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
                SDUtil.Log(files.Length.ToString());
                if (loraNames == null) loraNames = new List<string>();
                loraNames.Clear();
                foreach (var f in files)
                {
                    loraNames.Add(Path.GetFileNameWithoutExtension(f));
                }
            }
            catch (Exception)
            {
                SDUtil.LogError(url + " " + request.downloadHandler.text);
            }
#endif
        }


        public override object OnRequestValue(Port port)
        {
            prompt = GetInputValue("Prompt", this.prompt);
            loraPrompt = GetInputValue("LoRAPrompt", this.loraPrompt);
            if (!string.IsNullOrEmpty(loraPrompt))
            {
                loraPrompt = $"{loraPrompt},";
            }
            string result = $"{prompt},{loraPrompt}<lora:{lora}:{strength}>";
            return result;
        }
    }
}
