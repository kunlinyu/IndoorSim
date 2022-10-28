using System.Runtime.InteropServices;
using UnityEngine;

class PlatformInfo
{
    public string applicationPlatform;
    public string deviceModel;
    public string deviceName;
    public string deviceUniqueIdentifier;
    public string operatingSystemFamily;
    public string operatingSystem;
    public string graphicsDeviceName;
    public string processorType;
    public string graphicsDeviceVendor;
    public string graphicsDeviceVersion;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string DeviceUniqueIdentifier();
#endif

    static public PlatformInfo Get()
    {
        var platformInfo = new PlatformInfo()
        {
            applicationPlatform = Application.platform.ToString(),
            deviceModel = SystemInfo.deviceModel,
            deviceName = SystemInfo.deviceName,
            deviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier,
            operatingSystem = SystemInfo.operatingSystem,
            operatingSystemFamily = SystemInfo.operatingSystemFamily.ToString(),
            graphicsDeviceName = SystemInfo.graphicsDeviceName,
            graphicsDeviceVendor = SystemInfo.graphicsDeviceVendor,
            graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion,
            processorType = SystemInfo.processorType,
        };

#if UNITY_WEBGL && !UNITY_EDITOR
        platformInfo.deviceUniqueIdentifier = DeviceUniqueIdentifier();
        Debug.Log("call js to get deviceUniqueIdentifier: " + platformInfo.deviceUniqueIdentifier);
#endif

        return platformInfo;
    }
}