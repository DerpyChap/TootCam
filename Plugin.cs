using System.IO;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace TootCam;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("TromboneChampUnflattened.exe")]
public class Plugin : BaseUnityPlugin
{
    public static ConfigEntry<float> FOV;
    public static ConfigEntry<bool> MSAA;
    public static GameObject MainCamera;
    public static GameObject SpecCamera;
    public static ConfigEntry<float> RotationDampening;
    public static ConfigEntry<float> PositionDampening;
    public static ConfigEntry<bool> EnableDampening;

    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        var config = new ConfigFile(Path.Combine(Paths.ConfigPath, "tootcam.cfg"), true);

        FOV = config.Bind("General", "FOV Value", 70f, "Controls the camera's FOV.");
        MSAA = config.Bind("General", "Enable MSAA", false, "Turns MSAA on the camera on or off.");
        
        EnableDampening = config.Bind("Dampening", "Enable Dampening", false, "Turns movement dampening on or off.");
        RotationDampening = config.Bind(
            "Dampening", "Rotation Damping", 0.1f, "Controls the rotation dampening on a scale of 0 to 1. Lower values mean stronger dampening, with 1 effectively disabling it.");
        PositionDampening = config.Bind(
            "Dampening", "Position Damping", 0.1f, "Controls the position dampening on a scale of 0 to 1. Lower values mean stronger dampening, with 1 effectively disabling it.");

        new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
}

[HarmonyPatch(typeof(VRPlatformManager))]
[HarmonyPatch("Awake")]
internal class VRPlatformManagerAwakePatch
{
    static void Postfix(VRPlatformManager __instance)
    {
        Plugin.MainCamera = __instance.transform.GetChild(0).GetChild(0).gameObject;
        Plugin.SpecCamera = new("SpectatorCamera");
        var cam = Plugin.SpecCamera.AddComponent<Camera>();
        cam.stereoTargetEye = StereoTargetEyeMask.None;
        cam.depth = 100;
        cam.nearClipPlane = 0.01f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        
        // configurable values
        cam.fieldOfView = Plugin.FOV.Value;
        cam.allowMSAA = Plugin.MSAA.Value;
        
        Plugin.SpecCamera.transform.parent = __instance.transform;
        Plugin.Logger.LogInfo("Camera created!");
    }
}


[HarmonyPatch(typeof(VRPlatformManager))]
[HarmonyPatch("Update")]
internal class VRPlatformManagerUpdatePatch
{
    static void Postfix()
    {
        if (Plugin.EnableDampening.Value)
        {
            Plugin.SpecCamera.transform.position = Vector3.Lerp(
                Plugin.SpecCamera.transform.position, Plugin.MainCamera.transform.position,Plugin.PositionDampening.Value);
            Plugin.SpecCamera.transform.rotation = Quaternion.Lerp(
                Plugin.SpecCamera.transform.rotation, Plugin.MainCamera.transform.rotation, Plugin.RotationDampening.Value);
        }
        else
        {
            Plugin.SpecCamera.transform.position = Plugin.MainCamera.transform.position;
            Plugin.SpecCamera.transform.rotation = Plugin.MainCamera.transform.rotation;
        }
        
    }
}
