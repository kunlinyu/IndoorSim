using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LogWindow : MonoBehaviour
{

    private StyleColor logColor;
    private StyleColor warningColor;
    private StyleColor errorColor;
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        logColor = GetComponent<UIDocument>().rootVisualElement.Q<Label>("LogBackground").resolvedStyle.backgroundColor;
        warningColor = GetComponent<UIDocument>().rootVisualElement.Q<Label>("WarningBackground").resolvedStyle.backgroundColor;
        errorColor = GetComponent<UIDocument>().rootVisualElement.Q<Label>("ErrorBackground").resolvedStyle.backgroundColor;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        ListView listView = GetComponent<UIDocument>().rootVisualElement.Q<ListView>("LogList");

        Label label = new Label(type.ToString() + "[" + DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.ff") + "] " + logString);
        if (type == LogType.Log)
            label.style.backgroundColor = logColor;
        else if (type == LogType.Warning)
            label.style.backgroundColor = warningColor;
        else if (type == LogType.Error)
            label.style.backgroundColor = errorColor;
        listView.hierarchy.Add(label);
    }
}
