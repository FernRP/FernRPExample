using System.Collections;
using System.Collections.Generic;
using StableDiffusionGraph.SDGraph.Nodes;
using UnityEngine;
using UnityEngine.Rendering;

public class SDDataHandle : MonoBehaviour
{
    public static string serverURL = "http://127.0.0.1:7860";
    public static string ModelsAPI = "/sdapi/v1/sd-models";
    public static string TextToImageAPI = "/sdapi/v1/txt2img";
    public static string ImageToImageAPI = "/sdapi/v1/img2img";
    public static string OptionAPI = "/sdapi/v1/options";
    public static string ProgressAPI = "/sdapi/v1/progress";
    public static bool UseAuth = false;
    public static string Username = "";
    public static string Password = "";
    
    public static string[] samplers = new string[]{
        "Euler a", "Euler", "LMS", "Heun", "DPM2", "DPM2 a", "DPM++ 2S a", "DPM++ 2M", "DPM++ SDE", "DPM fast", "DPM adaptive",
        "LMS Karras", "DPM2 Karras", "DPM2 a Karras", "DPM++ 2S a Karras", "DPM++ 2M Karras", "DPM++ SDE Karras", "DDIM", "PLMS"
    };
}

public class SDTextureHandle
{
    private static Texture2D _refreshIcon;
    public static Texture2D RefreshIcon
    {
        get
        {
            if (_refreshIcon == null)
            {
                _refreshIcon = Resources.Load<Texture2D>("refresh");
            }
            return _refreshIcon;
        }
    }
}

public class SDUtil
{
    
    public static int[] ScaleList = new int[]{20, 25, 50, 75, 100};
    
    public static Vector2 GetMainGameViewSize()
    {
        System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        System.Object Res = GetSizeOfMainGameView.Invoke(null,null);
        return (Vector2)Res;
    }
}

public class SDModel
{
    public string title;
    public string model_name;
    public string hash;
    public string sha256;
    public string filename;
    public string config;
}

public class Prompt
{
    public string positive;
    public string negative;
}
