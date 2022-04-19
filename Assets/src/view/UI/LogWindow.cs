using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LogWindow : MonoBehaviour
{
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        ListView listView = GetComponent<UIDocument>().rootVisualElement.Q<ListView>("LogList");
        listView.hierarchy.Add(new Label(type.ToString() + "[" + DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.ff") + "] " + logString));
    }
}
