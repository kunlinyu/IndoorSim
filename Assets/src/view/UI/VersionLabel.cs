using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class VersionLabel : MonoBehaviour
{
    public UIDocument rootUIDocument;
    void Start()
    {
        Label versionLabel = rootUIDocument.rootVisualElement.Q<Label>("VersionLabel");
        versionLabel.text = " IndoorSim version: V" + Application.version;
    }
}
