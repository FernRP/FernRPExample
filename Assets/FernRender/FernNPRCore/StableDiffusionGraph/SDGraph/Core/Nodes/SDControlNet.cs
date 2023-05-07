using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FernGraph;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Networking;

namespace FernNPRCore.StableDiffusionGraph
{
    [Node(Path = "Standard")]
    [Tags("SD Node")]
    public class SDControlNet : SDFlowNode, ICanExecuteSDFlow
    {
        [Input("Image")] public Texture2D controlNetImg;
        public string module = "none";
        public string model = "none";
        [Input] public float weight = 1;
        [Input] public ResizeMode resize_mode = ResizeMode.ScaleToFit_InnerFit;
        [Input] public bool lowvram = false;
        [Input] public int processor_res = 64;
        [Input] public int threshold_a = 64;
        [Input] public int threshold_b = 64;
        [Input] public float guidance_start = 0.0f;
        [Input] public float guidance_end = 1.0f;
        [Input] public float guidance = 1f;
        [Input] public ControlMode control_mode = ControlMode.Balanced;
        [Output("ControlNet")] public ControlNetData controlNet;

        public List<string> modelList = new List<string>();
        public int currentModelListIndex = 0;
        
        public List<string> moudleList = new List<string>();
        public int currentMoudleListIndex = 0;

        public override void OnAddedToGraph()
        {
            base.OnAddedToGraph();
            controlNet = new ControlNetData();
            EditorCoroutineUtility.StartCoroutine(ControlNetModelListAsync(), this);
            EditorCoroutineUtility.StartCoroutine(ControlNetMoudleList(), this);
        }

        public override object OnRequestValue(Port port)
        {
            return controlNet;
        }
        
        /// <summary>
        /// Get the list of available Stable Diffusion models.
        /// </summary>
        /// <returns></returns>
        IEnumerator ControlNetModelListAsync()
        {
            // Stable diffusion API url for getting the models list
            string url = SDDataHandle.Instance.GetServerURL() + SDDataHandle.Instance.ControlNetModelList;

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
                // Deserialize the response to a class
                ControlNetModel ms = JsonConvert.DeserializeObject<ControlNetModel>(request.downloadHandler.text);
                modelList.Clear();

                foreach (var m in ms.model_list)
                {
                    modelList.Add(m);
                }

                model = modelList[0];
            }
            catch (Exception)
            {
                Debug.Log("Server needs and API key authentication. Please check your settings!");
            }
        }
        
        /// <summary>
        /// Get the list of available Stable Diffusion models.
        /// </summary>
        /// <returns></returns>
        IEnumerator ControlNetMoudleList()
        {
            // Stable diffusion API url for getting the models list
            string url = SDDataHandle.Instance.GetServerURL() + SDDataHandle.Instance.ControlNetMoudleList;

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
                // Deserialize the response to a class
                ControlNetMoudle ms = JsonConvert.DeserializeObject<ControlNetMoudle>(request.downloadHandler.text);
                moudleList.Clear();

                foreach (var m in ms.module_list)
                {
                    moudleList.Add(m);
                }

                module = moudleList[0];
            }
            catch (Exception)
            {
                Debug.Log("Server needs and API key authentication. Please check your settings!");
            }
        }
        
        /// <summary>
        /// Get the list of available Stable Diffusion models.
        /// </summary>
        /// <returns></returns>
        IEnumerator ControlNetDetect()
        {
            if (model.Equals("none"))
            {
                yield return ControlNetModelListAsync();
            }
            if (module.Equals("none"))
            {
                yield return ControlNetMoudleList();
            }
            
            controlNet.module = module;
            controlNet.model = model;
            controlNet.weight = weight;
            controlNet.resize_mode = (int)resize_mode;
            controlNet.lowvram = lowvram;
            controlNet.processor_res = processor_res;
            controlNet.threshold_a = threshold_a;
            controlNet.threshold_b = threshold_b;
            controlNet.guidance_start = guidance_start;
            controlNet.guidance_end = guidance_end;
            controlNet.guidance = guidance;
            controlNet.control_mode = (int)control_mode;
            if (controlNetImg != null)
            {
                byte[] inputImgBytes = controlNetImg.EncodeToPNG();
                string inputImgString = Convert.ToBase64String(inputImgBytes);
                controlNet.input_image = inputImgString;
            }
            
            // // Stable diffusion API url for getting the models list
            // HttpWebRequest httpWebRequest = null;
            // try
            // {
            //     string url = SDDataHandle.Instance.serverURL + SDDataHandle.Instance.ControlNetDetect;
            //     httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            //     httpWebRequest.ContentType = "application/json";
            //     httpWebRequest.Method = "POST";
            //     if (SDDataHandle.Instance.UseAuth && !SDDataHandle.Instance.Username.Equals("") && !SDDataHandle.Instance.Password.Equals(""))
            //     {
            //         Debug.Log("Using API key to authenticate");
            //         byte[] bytesToEncode = Encoding.UTF8.GetBytes(SDDataHandle.Instance.Username + ":" + SDDataHandle.Instance.Password);
            //         string encodedCredentials = Convert.ToBase64String(bytesToEncode);
            //         httpWebRequest.Headers.Add("Authorization", "Basic " + encodedCredentials);
            //     }
            //     
            //     using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            //     {
            //         ControlNetDetect detect = new ControlNetDetect();
            //         
            //         byte[] inputImgBytes = controlNetImg.EncodeToPNG();
            //         string inputImgString = Convert.ToBase64String(inputImgBytes);
            //         detect.controlnet_input_images = new []{inputImgString};
            //         detect.controlnet_module = module;
            //         string json = JsonConvert.SerializeObject(detect);
            //         Debug.Log(json);
            //         streamWriter.Write(json);
            //     }
            // }            
            // catch (Exception e)
            // {
            //     Debug.LogError(e.Message + "\n\n" + e.StackTrace);
            // }
            //
            // // Read the output of generation
            // if (httpWebRequest != null)
            // {
            //     // Wait that the generation is complete before procedding
            //     Task<WebResponse> webResponse = httpWebRequest.GetResponseAsync();
            //     while (!webResponse.IsCompleted)
            //     {
            //         if (SDDataHandle.Instance.UseAuth && !SDDataHandle.Instance.Username.Equals("") && !SDDataHandle.Instance.Password.Equals(""))
            //             //UpdateGenerationProgressWithAuth();
            //             // else
            //             // UpdateGenerationProgress();
            //
            //             yield return new WaitForSeconds(0.5f);
            //     }
            //     // Stream the result from the server
            //     var httpResponse = webResponse.Result;
            //     using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            //     {
            //         // Decode the response as a JSON string
            //         string result = streamReader.ReadToEnd();
            //         Debug.Log(result);
            //     }
            //     yield return null;
            // }

            yield return null;
        }

        public override IEnumerator Execute()
        {
            if (controlNet == null) controlNet = new ControlNetData();
            controlNetImg = GetInputValue("Image", this.controlNetImg);
            if (modelList == null || modelList.Count <= 0)
            {
                yield return ControlNetModelListAsync();
            }

            if (moudleList == null || moudleList.Count <= 0)
            {
                yield return ControlNetMoudleList();
            }
            
            yield return ControlNetDetect();
        }
    }
}
