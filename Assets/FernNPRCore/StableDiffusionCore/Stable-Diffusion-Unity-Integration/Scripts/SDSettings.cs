using UnityEditor.Profiling.Memory.Experimental;
using UnityEngine;

/// <summary>
/// Data structure for specifying settings of Stable Diffusion server API or 
/// default settings to use when adding new StableDiffusionMaterial or StableDIffusionImage 
/// to a Unity GameObject.
/// </summary>
public class SDSettings : ScriptableObject
{
    [Header("AUTOMATIC1111 Settings")]
    public string StableDiffusionServerURL = "http://127.0.0.1:7860";
    public string ModelsAPI = "/sdapi/v1/sd-models";
    public string TextToImageAPI = "/sdapi/v1/txt2img";
    public string ImageToImageAPI = "/sdapi/v1/img2img";
    public string OptionAPI = "/sdapi/v1/options";
    public string ProgressAPI = "/sdapi/v1/progress";
    public string OutputFolder = "/streamingAssets";
    public string sampler = "Euler a";
    public int width = 512;
    public int height = 512;
    public int steps = 35;
    public float cfgScale = 7;
    public long seed = -1;
    
    [Header("API Settings")]
    public bool useAuth = false;
    public string user = "";
    public string pass = "";
    
    [Header("URP Settings")]
    public bool useUniversalRenderPipeline = false;
}

/// <summary>
/// Data structure to easily serialize the parameters to send
/// to the Stable Diffusion server when generating an image via Txt2Img.
/// </summary>
class SDParamsInTxt2Img
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
    public bool override_settings_restore_afterwards = true;
    public string sampler_index = "Euler";
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

/// <summary>
/// Data structure to easily serialize the parameters to send
/// to the Stable Diffusion server when generating an image via Img2Img.
/// </summary>
class SDParamsInImg2Img
{
    public string[] init_images = { "" };
    public int resize_mode = 0;
    public float denoising_strength = 0.5f;
    public string mask = ""; // mask需要是一张图片！
    public int mask_blur = 4;
    public int inpainting_fill = 1;
    public bool inpaint_full_res = true;
    public int inpaint_full_res_padding = 0;
    public int inpainting_mask_invert = 0; // 0 表示只生成黑色区域，其他区域不变
    public int initial_noise_multiplier = 1; // if 0, output image looks more blurry
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


/// <summary>
/// Data structure to help serialize into a JSON the model to be used by Stable Diffusion.
/// This is to send along a Set Option API request to the server.
/// </summary>
class SDOption
{
    public string sd_model_checkpoint = "";
}

/// <summary>
/// Data structure to help deserialize from a JSON the state of the progress of an image generation.
/// </summary>
class SDProgressState
{
    public bool skipped;
    public bool interrupted;
    public string job;
    public int job_count;
    public string job_timestamp;
    public int job_no;
    public int sampling_step;
    public int sampling_steps;
}

/// <summary>
/// Data structure to help deserialize from a JSON the progress status of an image generation.
/// </summary>
class SDProgress
{
    public float progress;
    public float eta_relative;
    public SDProgressState state;
    public string current_image;
    public string textinfo;
}

