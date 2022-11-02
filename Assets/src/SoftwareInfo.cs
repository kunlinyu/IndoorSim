using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SoftwareInfo
{
    public string absoluteURL;
    public string buildGUID;
    public bool isEditor;
    public string platform;
    public string systemLanguage;
    public string unityVersion;
    public string version;

    static public SoftwareInfo Get()
    {
        return new SoftwareInfo()
        {
            absoluteURL = Application.absoluteURL,
            buildGUID = Application.buildGUID,
            isEditor = Application.isEditor,
            platform = Application.platform.ToString(),
            systemLanguage = Application.systemLanguage.ToString(),
            unityVersion = Application.unityVersion,
            version = Application.version,
        };
    }
}


