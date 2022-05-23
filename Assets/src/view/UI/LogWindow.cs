using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LogWindow : MonoBehaviour
{
    private const int kMaxLogCount = 100;

    private List<string> logs = new List<string>();
    ListView listView;

    void Start()
    {
        Application.logMessageReceived += HandleLog;
    }

    public void Init(ListView listView)
    {
        this.listView = listView;

        this.listView.itemsSource = logs;

        this.listView.makeItem = () => new Label();

        this.listView.bindItem = (label, index) =>
        {
            string log = logs[index];
            ((Label)label).text = log;
            label.name = LogType.Log.ToString();

            if (log.StartsWith(LogType.Log.ToString()))
                label.name = LogType.Log.ToString();
            else if (log.StartsWith(LogType.Warning.ToString()))
                label.name = LogType.Warning.ToString();
            else if (log.StartsWith(LogType.Error.ToString()))
                label.name = LogType.Error.ToString();

            label.tooltip = "double click to copy";
        };

        this.listView.onItemsChosen += (items) =>
        {
            if (items.ToList().Count == 1)
            {
                CopyText((string)items.First());
                Thread.Sleep(300);
                Debug.Log($"Copy to clipboard: \"{(string)items.First()}\"");
            }
        };
    }

    private void CopyText(string textToCopy)
    {
        TextEditor editor = new TextEditor { text = textToCopy };
        editor.SelectAll();
        editor.Copy();
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string logWithHeader = type.ToString() + "[" + DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss\.ff") + "] " + logString;
        logs.Add(logWithHeader);
        while (logs.Count > kMaxLogCount)
            logs.RemoveAt(0);

        listView.Rebuild();
        listView.ScrollToItemById(logs.Count - 1);
    }
}

