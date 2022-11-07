using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class VersionLabel : MonoBehaviour
{
    void Start()
    {
        var platformInfo = PlatformInfo.Get();

        string version = "IndoorSim version: V" + Application.version;

        string schemaHash = "schema hash: " + IndoorSimData.JSchemaHash();

        string DUID = "device unique ID: " + platformInfo.deviceUniqueIdentifier;

        List<string> strings = new() {  version, schemaHash, DUID };
        

        Label versionLabel = GetComponent<UIDocument>().rootVisualElement.Q<Label>("VersionLabel");
        versionLabel.text = " " + string.Join("\n ", strings);

    }
}
