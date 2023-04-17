using System;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Global Stable Diffusion parameters configuration.
/// </summary>
[ExecuteInEditMode]
public class StableDiffusionConfiguration : MonoBehaviour
{
    [SerializeField] 
    public SDSettings settings;

    [SerializeField]
    public string[] samplers = new string[]{
        "Euler a", "Euler", "LMS", "Heun", "DPM2", "DPM2 a", "DPM++ 2S a", "DPM++ 2M", "DPM++ SDE", "DPM fast", "DPM adaptive",
        "LMS Karras", "DPM2 Karras", "DPM2 a Karras", "DPM++ 2S a Karras", "DPM++ 2M Karras", "DPM++ SDE Karras", "DDIM", "PLMS"
    };

    [SerializeField]
    public string[] modelNames;

    /// <summary>
    /// Data structure that represents a Stable Diffusion model to help deserialize from JSON string.
    /// </summary>
    class Model
    {
        public string title;
        public string model_name;
        public string hash;
        public string sha256;
        public string filename;
        public string config;
    }

    /// <summary>
    /// Method called when the user click on List Model from the inspector.
    /// </summary>
    public void ListModels()
    {
        StartCoroutine(ListModelsAsync());
    }

    /// <summary>
    /// Get the list of available Stable Diffusion models.
    /// </summary>
    /// <returns></returns>
    IEnumerator ListModelsAsync()
    {
        // Stable diffusion API url for getting the models list
        string url = settings.StableDiffusionServerURL + settings.ModelsAPI;

        UnityWebRequest request = new UnityWebRequest(url, "GET");
        request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        if (settings.useAuth && !settings.user.Equals("") && !settings.pass.Equals(""))
        {
            Debug.Log("Using API key to authenticate");
            byte[] bytesToEncode = Encoding.UTF8.GetBytes(settings.user + ":" + settings.pass);
            string encodedCredentials = Convert.ToBase64String(bytesToEncode);
            request.SetRequestHeader("Authorization", "Basic " + encodedCredentials);
        }
        
        yield return request.SendWebRequest();

        try
        {
            // Deserialize the response to a class
            Model[] ms = JsonConvert.DeserializeObject<Model[]>(request.downloadHandler.text);

            // Keep only the names of the models
            List<string> modelsNames = new List<string>();

            foreach (Model m in ms) 
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

    /// <summary>
    /// Set a model to use by Stable Diffusion.
    /// </summary>
    /// <param name="modelName">Model to set</param>
    /// <returns></returns>
    public IEnumerator SetModelAsync(string modelName)
    {
        // Stable diffusion API url for setting a model
        string url = settings.StableDiffusionServerURL + settings.OptionAPI;

        // Load the list of models if not filled already
        if (modelNames == null || modelNames.Length == 0)
            yield return ListModelsAsync();

        try
        {
            // Tell Stable Diffusion to use the specified model using an HTTP POST request
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            // add auth-header to request
            if (settings.useAuth && !settings.user.Equals("") && !settings.pass.Equals(""))
            {
                httpWebRequest.PreAuthenticate = true;
                byte[] bytesToEncode = Encoding.UTF8.GetBytes(settings.user + ":" + settings.pass);
                string encodedCredentials = Convert.ToBase64String(bytesToEncode);
                httpWebRequest.Headers.Add("Authorization", "Basic " + encodedCredentials);
            }
            
            // Write to the stream the JSON parameters to set a model
            if (httpWebRequest != null)
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

}
