using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LogWindow : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    public UIDocument rootUIDocument;
    private const int kMaxLogCount = 100;
    private const int kMaxLogLength = 200;

    private List<string> logs = new List<string>();
    ListView listView;

    void Start()
    {
        Init();
        Application.logMessageReceived += HandleLog;

        VisualElement logWindow = rootUIDocument.rootVisualElement.Q<VisualElement>("LogWindow");
        logWindow.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(this, new UIEvent() { name = "sim panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        logWindow.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(this, new UIEvent() { name = "sim panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
    }

    public void Init()
    {
        this.listView = rootUIDocument.rootVisualElement.Q<ListView>("LogList");

        this.listView.selectionType = SelectionType.Multiple;

        this.listView.itemsSource = logs;

        this.listView.makeItem = () => new Label();

        this.listView.bindItem = (label, index) =>
        {
            string logPrefix = logs[index];
            if (logPrefix.Length > kMaxLogLength)
                logPrefix = logPrefix.Substring(0, kMaxLogLength);

            ((Label)label).text = logPrefix;

            label.userData = index;

            if (logPrefix.StartsWith(LogType.Log.ToString()))
                label.name = LogType.Log.ToString();
            else if (logPrefix.StartsWith(LogType.Warning.ToString()))
                label.name = LogType.Warning.ToString();
            else if (logPrefix.StartsWith(LogType.Error.ToString()))
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
            else
            {
                Debug.Log("don't support multiple select");
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

