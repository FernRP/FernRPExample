using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FernNPRCore.StableDiffusionGraph
{
    public class SDDataHandle : MonoBehaviour
    {
        public static string serverURL = "http://127.0.0.1:7860";
        public static string ModelsAPI = "/sdapi/v1/sd-models";
        public static string ControlNetTex2Img = "/controlnet/txt2img";
        public static string ControlNetModelList = "/controlnet/model_list";
        public static string ControlNetMoudleList = "/controlnet/module_list";
        public static string ControlNetDetect = "/controlnet/detect";
        public static string TextToImageAPI = "/sdapi/v1/txt2img";
        public static string ImageToImageAPI = "/sdapi/v1/img2img";
        public static string OptionAPI = "/sdapi/v1/options";
        public static string DataDirAPI = "/sdapi/v1/cmd-flags";
        public static string ProgressAPI = "/sdapi/v1/progress";
        public static bool UseAuth = false;
        public static string Username = "";
        public static string Password = "";

        public static string[] samplers = new string[]
        {
            "Euler a", "Euler", "LMS", "Heun", "DPM2", "DPM2 a", "DPM++ 2S a", "DPM++ 2M", "DPM++ SDE", "DPM fast",
            "DPM adaptive",
            "LMS Karras", "DPM2 Karras", "DPM2 a Karras", "DPM++ 2S a Karras", "DPM++ 2M Karras", "DPM++ SDE Karras",
            "DDIM", "PLMS"
        };

        public static string modelUse;
        public static string loraUse;
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
                    _refreshIcon = Resources.Load<Texture2D>("Icon/refresh");
                }

                return _refreshIcon;
            }
        }

        private static Texture2D _saveIcon;

        public static Texture2D SaveIcon
        {
            get
            {
                if (_saveIcon == null)
                {
                    _saveIcon = Resources.Load<Texture2D>("Icon/save");
                }

                return _saveIcon;
            }
        }
    }

    public class SDUtil
    {
        public static int[] ScaleList = new int[] { 25, 50, 75, 100 };
        private const string LOG = "Fern SD Graph: ";

        public static Vector2 GetMainGameViewSize()
        {
            System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
            return (Vector2)Res;
        }

        public static void SetToNone(string assetPath)
        {
            assetPath = GetAssetPath(assetPath);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Debug.Log(assetPath);
            if (importer != null)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.isReadable = true;
                AssetDatabase.Refresh();
            }
        }

        public static void SDLog(string log)
        {
            Debug.Log($"{LOG}{log}");
        }

        public static void SDWarnning(string log)
        {
            // TODO
        }

        public static void SDError(string log)
        {
            // TODO
        }

        public static string GetAssetPath(string absolutePath)
        {
            string relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
            return relativePath;
        }

        public static void SaveAsLinearPNG(Texture2D texture, string filePath)
        {
            // 将Texture2D转换为伽马空间
            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = pixels[i].gamma;
            }

            Texture2D gammaTexture = new Texture2D(texture.width, texture.height);
            gammaTexture.SetPixels(pixels);
            gammaTexture.Apply();

            // 将伽马空间的Texture2D编码为PNG
            byte[] pngData = gammaTexture.EncodeToPNG();
            File.WriteAllBytes(filePath, pngData);
        }
    }


    /// <summary>
    /// Data structure to easily serialize the parameters to send
    /// to the Stable Diffusion server when generating an image via Txt2Img.
    /// </summary>
    class SDParamsInTxt2ImgContronlNet
    {
        public bool enable_hr = false;
        public float denoising_strength = 0.75f;
        public int firstphase_width = 0;
        public int firstphase_height = 0;
        public float hr_scale = 2;
        public string hr_upscaler = "";
        public int hr_second_pass_steps = 0;
        public int hr_resize_x = 0;
        public int hr_resize_y = 0;
        public string prompt = "";
        public string[] styles = { "" };
        public long seed = -1;
        public long subseed = -1;
        public float subseed_strength = 0;
        public int seed_resize_from_h = -1;
        public int seed_resize_from_w = -1;
        public string sampler_name = "Euler a";
        public int batch_size = 1;
        public int n_iter = 1;
        public int steps = 50;
        public float cfg_scale = 7;
        public int width = 512;
        public int height = 512;
        public bool restore_faces = false;
        public bool tiling = false;
        public string negative_prompt = "";
        public float eta = 0;
        public float s_churn = 0;
        public float s_tmax = 0;
        public float s_tmin = 0;
        public float s_noise = 1;
        public bool override_settings_restore_afterwards = true;
        public string sampler_index = "Euler";
        public ALWAYSONSCRIPTS alwayson_scripts = null;
    }

    class SDParamsInTxt2Img
    {
        public bool enable_hr = false;
        public float denoising_strength = 0.75f;
        public int firstphase_width = 0;
        public int firstphase_height = 0;
        public float hr_scale = 2;
        public string hr_upscaler = "";
        public int hr_second_pass_steps = 0;
        public int hr_resize_x = 0;
        public int hr_resize_y = 0;
        public string prompt = "";
        public string[] styles = { "" };
        public long seed = -1;
        public long subseed = -1;
        public float subseed_strength = 0;
        public int seed_resize_from_h = -1;
        public int seed_resize_from_w = -1;
        public string sampler_name = "Euler a";
        public int batch_size = 1;
        public int n_iter = 1;
        public int steps = 50;
        public float cfg_scale = 7;
        public int width = 512;
        public int height = 512;
        public bool restore_faces = false;
        public bool tiling = false;
        public string negative_prompt = "";
        public float eta = 0;
        public float s_churn = 0;
        public float s_tmax = 0;
        public float s_tmin = 0;
        public float s_noise = 1;
        public bool override_settings_restore_afterwards = true;
        public string sampler_index = "Euler";
    }

    class ControlNetDetect
    {
        public string controlnet_module = "none";
        public string[] controlnet_input_images = new[] { "" };
        public int controlnet_processor_res = 64;
        public int controlnet_threshold_a = 64;
        public int controlnet_threshold_b = 64;
    }

    /// <summary>
    /// Data structure to easily deserialize the data returned
    /// by the Stable Diffusion server after generating an image via Txt2Img.
    /// </summary>
    class SDParamsOutTxt2Img
    {
        public bool enable_hr = false;
        public float denoising_strength = 0;
        public int firstphase_width = 0;
        public int firstphase_height = 0;
        public float hr_scale = 2;
        public string hr_upscaler = "";
        public int hr_second_pass_steps = 0;
        public int hr_resize_x = 0;
        public int hr_resize_y = 0;
        public string prompt = "";
        public string[] styles = { "" };
        public long seed = -1;
        public long subseed = -1;
        public float subseed_strength = 0;
        public int seed_resize_from_h = -1;
        public int seed_resize_from_w = -1;
        public string sampler_name = "Euler a";
        public int batch_size = 1;
        public int n_iter = 1;
        public int steps = 50;
        public float cfg_scale = 7;
        public int width = 512;
        public int height = 512;
        public bool restore_faces = false;
        public bool tiling = false;
        public string negative_prompt = "";
        public float eta = 0;
        public float s_churn = 0;
        public float s_tmax = 0;
        public float s_tmin = 0;
        public float s_noise = 1;
        public SettingsOveride override_settings;
        public bool override_settings_restore_afterwards = true;
        public string[] script_args = { };
        public string sampler_index = "Euler";
        public string script_name = "";

        public class SettingsOveride
        {
        }
    }

    class SDParamsInImg2Img
    {
        public string[] init_images = { "" };
        public int resize_mode = 0;

        public float denoising_strength = 0.5f;

        //public string mask = null; 
        public int mask_blur = 4;
        public int inpainting_fill = 1;
        public bool inpaint_full_res = true;
        public int inpaint_full_res_padding = 0;
        public int inpainting_mask_invert = 0;
        public int initial_noise_multiplier = 1;
        public string prompt = "";
        public string[] styles = { "" };
        public long seed = -1;
        public long subseed = -1;
        public int subseed_strength = 0;
        public int seed_resize_from_h = -1;
        public int seed_resize_from_w = -1;
        public string sampler_name = "Euler a";
        public int batch_size = 1;
        public int n_iter = 1;
        public int steps = 50;
        public float cfg_scale = 7;
        public int width = 512;
        public int height = 512;
        public bool restore_faces = false;
        public bool tiling = false;
        public string negative_prompt = "";
        public float eta = 0;
        public float s_churn = 0;
        public float s_tmax = 0;
        public float s_tmin = 0;
        public float s_noise = 1;
        public SettingsOveride override_settings;
        public bool override_settings_restore_afterwards = true;
        public string[] script_args = { };
        public string sampler_index = "Euler";

        public bool include_init_images = false;

        //    public string script_name = ""; // including this throws a 422 Unprocessable Entity error
        public class SettingsOveride
        {
        }
    }

    class SDParamsInImg2ImgMask : SDParamsInImg2Img
    {
        public string mask = "";
    }


    class SDParamsInImg2ImgControlNet : SDParamsInImg2Img
    {
        public string mask = null;
        public ALWAYSONSCRIPTS alwayson_scripts = null;
    }

    /// <summary>
    /// Data structure to easily deserialize the data returned
    /// by the Stable Diffusion server after generating an image via Img2Img.
    /// </summary>
    class SDParamsOutImg2Img
    {
        public string[] init_images = { "" };
        public float resize_mode = 0;
        public float denoising_strength = 0.75f;
        public string mask = null;
        public float mask_blur = 10;
        public float inpainting_fill = 0;
        public bool inpaint_full_res = true;
        public float inpaint_full_res_padding = 32;
        public float inpainting_mask_invert = 0;
        public float initial_noise_multiplier = 0;
        public string prompt = "";
        public string[] styles = { "" };
        public long seed = -1;
        public long subseed = -1;
        public float subseed_strength = 0;
        public float seed_resize_from_h = -1;
        public float seed_resize_from_w = -1;
        public string sampler_name = "";
        public float batch_size = 1;
        public float n_iter = 1;
        public int steps = 50;
        public float cfg_scale = 7;
        public int width = 512;
        public int height = 512;
        public bool restore_faces = false;
        public bool tiling = false;
        public string negative_prompt = "";
        public float eta = 0;
        public float s_churn = 0;
        public float s_tmax = 0;
        public float s_tmin = 0;
        public float s_noise = 1;
        public SettingsOveride override_settings;
        public bool override_settings_restore_afterwards = true;
        public string[] script_args = { };
        public string sampler_index = "Euler";
        public bool include_init_images = false;
        public string script_name = "";

        public class SettingsOveride
        {
        }
    }

    /// <summary>
    /// Data structure to help serialize into a JSON the model to be used by Stable Diffusion.
    /// This is to send along a Set Option API request to the server.
    /// </summary>
    class SDOption
    {
        public string sd_model_checkpoint = "";
    }

    /// <summary>
    /// Data structure to easily deserialize the JSON response returned
    /// by the Stable Diffusion server after generating an image via Txt2Img.
    ///
    /// It will contain the generated images (in Ascii Byte64 format) and
    /// the parameters used by Stable Diffusion.
    /// 
    /// Note that the out parameters returned should be almost identical to the in
    /// parameters that you have submitted to the server for image generation, 
    /// to the exception of the seed which will contain the value of the seed used 
    /// for the generation if you have used -1 for value (random).
    /// </summary>
    class SDResponseTxt2Img
    {
        public string[] images;
        public SDParamsOutTxt2Img parameters;
        public string info;
    }

    /// <summary>
    /// Data structure to easily deserialize the JSON response returned
    /// by the Stable Diffusion server after generating an image via Img2Img.
    ///
    /// It will contain the generated images (in Ascii Byte64 format) and
    /// the parameters used by Stable Diffusion.
    /// 
    /// Note that the out parameters returned should be almost identical to the in
    /// parameters that you have submitted to the server for image generation, 
    /// to the exception of the seed which will contain the value of the seed used 
    /// for the generation if you have used -1 for value (random).
    /// </summary>
    class SDResponseImg2Img
    {
        public string[] images;
        public SDParamsOutImg2Img parameters;
        public string info;
    }

    public enum ResizeMode
    {
        JustResize = 0,
        ScaleToFit_InnerFit = 1,
        Envelope_OuterFit = 2
    }

    public enum ControlMode
    {
        Balanced = 0,
        My_prompt_is_more_important = 1,
        ControlNet_is_more_important = 2
    }

    public class ControlNetData
    {
        public string input_image = "";
        public string mask = "";
        public string module = "none";
        public string model;
        public float weight = 1;
        public int resize_mode = 1;
        public bool lowvram = false;
        public int processor_res = 64;
        public int threshold_a = 64;
        public int threshold_b = 64;
        public float guidance_start = 0.0f;
        public float guidance_end = 1.0f;
        public float guidance = 1f;
        public int control_mode = 0;
    }

    public class ControlNetDataArgs
    {
        public ControlNetData[] args;
    }


    public class ALWAYSONSCRIPTS
    {
        public ControlNetDataArgs controlnet;
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

    public class SDDataDir
    {
        public string data_dir;
        public string lora_dir;
    }

    public class ControlNetModel
    {
        public string[] model_list;
    }

    public class ControlNetMoudle
    {
        public string[] module_list;
    }

    public class Prompt
    {
        public string positive;
        public string negative;
    }
}