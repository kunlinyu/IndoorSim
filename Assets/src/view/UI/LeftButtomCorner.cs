using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LeftButtomCorner : MonoBehaviour
{
    void Start()
    {
        var platformInfo = PlatformInfo.Get();

        string version = "IndoorSim version: V" + Application.version;

        string schemaHash = "schema hash: " + IndoorSimData.JSchemaHash()[..7];

        string DUID = "device unique ID: " + platformInfo.deviceUniqueIdentifier[..7];

#if UNITY_EDITOR
        string build = "build: Editor";
#elif DEVELOPMENT_BUILD
        string build = "build: Development";
#else
        string build = "build: Release";
#endif

        List<string> strings = new() {  version, schemaHash, DUID, build };
        

        Label versionLabel = GetComponent<UIDocument>().rootVisualElement.Q<Label>("VersionLabel");
        versionLabel.text = " " + string.Join("\n ", strings);

    }
}
