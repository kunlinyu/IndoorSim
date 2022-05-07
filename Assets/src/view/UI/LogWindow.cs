using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LogWindow : MonoBehaviour
{

    private StyleColor logColor;
    private StyleColor warningColor;
    private StyleColor errorColor;
    private const int kMaxLogCount = 20;
    ListView listView;

    void Start()
    {
        Application.logMessageReceived += HandleLog;
    }

    public void Init(ListView listView, StyleColor logColor, StyleColor warningColor, StyleColor errorColor)
    {
        this.listView = listView;
        this.logColor = logColor;
        this.warningColor = warningColor;
        this.errorColor = errorColor;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        Label label = new Label(type.ToString() + "[" + DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.ff") + "] " + logString);
        if (type == LogType.Log)
            label.style.backgroundColor = logColor;
        else if (type == LogType.Warning)
            label.style.backgroundColor = warningColor;
        else if (type == LogType.Error)
            label.style.backgroundColor = errorColor;

        listView.hierarchy.Add(label);
        while (listView.hierarchy.childCount > kMaxLogCount)
            listView.hierarchy.RemoveAt(0);
    }
}

