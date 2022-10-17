using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class VersionLabel : MonoBehaviour
{
    void Start()
    {
        Label versionLabel = GetComponent<UIDocument>().rootVisualElement.Q<Label>("VersionLabel");
        versionLabel.text = " IndoorSim version: V" + Application.version + "\n schema hash: " + IndoorSimData.JSchemaHash();
    }
}
