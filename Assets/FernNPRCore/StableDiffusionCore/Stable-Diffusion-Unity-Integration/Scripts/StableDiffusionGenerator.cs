using System;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class StableDiffusionGenerator : MonoBehaviour
{
    static protected StableDiffusionConfiguration sdc = null;
    private Coroutine _updateProgressRunning = null;
    
    /// <summary>
    /// Update a generation progress bar
    /// </summary>
    protected void UpdateGenerationProgress()
    {
#if UNITY_EDITOR
        // Stable diffusion API url for setting a model
        string url = sdc.settings.StableDiffusionServerURL + sdc.settings.ProgressAPI;

        float progress = 0;

        using (WebClient client = new WebClient())
        {
            // Send the GET request
            string responseBody = client.DownloadString(url);

            // Deserialize the response to a class
            SDProgress sdp = JsonConvert.DeserializeObject<SDProgress>(responseBody);
            progress = sdp.progress;

            EditorUtility.DisplayProgressBar("Generation in progress", (progress*100).ToString("F1") + "%", progress);
        }
#endif
    }

    /// <summary>
    /// Update a generation progress bar with auth
    /// </summary>
    protected void UpdateGenerationProgressWithAuth()
    {
        #if UNITY_EDITOR
            if (_updateProgressRunning != null) return;
            _updateProgressRunning = StartCoroutine(UpdateGenerationProgressWithAuthCor());
        #endif
    }
    
    private IEnumerator UpdateGenerationProgressWithAuthCor()
    {
        // Stable diffusion API url for setting a model
        string url = sdc.settings.StableDiffusionServerURL + sdc.settings.ProgressAPI;
        float progress = 0;

        using (UnityWebRequest modelInfoRequest = UnityWebRequest.Get(url))
        {
            byte[] bytesToEncode = Encoding.UTF8.GetBytes(sdc.settings.user + ":" + sdc.settings.pass);
            string encodedText = Convert.ToBase64String(bytesToEncode);

            modelInfoRequest.SetRequestHeader("Authorization", "Basic " + encodedText);
            yield return modelInfoRequest.SendWebRequest();

            // Deserialize the response to a class
            SDProgress sdp = JsonConvert.DeserializeObject<SDProgress>(modelInfoRequest.downloadHandler.text);
            progress = sdp.progress;

            EditorUtility.DisplayProgressBar("Generation in progress", progress * 100 + "%", progress);
        }

        _updateProgressRunning = null;
        yield return null;
    }
    
    /// <summary>
    /// Find all the game objects that contains a certain component type.
    /// </summary>
    /// <typeparam name="T">Type of component to search for</typeparam>
    /// <param name="g">Game object for which to search it's children</param>
    /// <param name="active">The game object must be active (true) or can also be not active (false)</param>
    /// <returns>Array of game object found, all of which containing a component of the specified type</returns>
    public static T[] FindInChildrenAll<T>(GameObject g, bool active = true) where T : class
    {
        List<T> list = new List<T>();

        // Search in all the children of the specified game object
        foreach (Transform t in g.transform)
        {
            // GameObject has no children, skip it
            if (t == null)
                continue;

            if (active && !t.gameObject.activeSelf)
                continue;

            // Found one, check component
            T comp = t.GetComponent<T>();
            if (comp != null && comp.ToString() != "null")
                list.Add(comp);

            // Recursively search into the children of this game object
            T[] compo = FindInChildrenAll<T>(t.gameObject);
            if (compo != null && compo.Length > 0)
            {
                foreach (T tt in compo)
                    list.Add(tt);
            }
        }

        // Not found, return null
        return list.ToArray();
    }
}
