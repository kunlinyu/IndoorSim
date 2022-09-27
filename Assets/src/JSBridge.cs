using System.Collections.Generic;
using UnityEngine;

public class JSBridge : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    // private UIEventSubscriber eventSubscriber;

    void SendJsonMessage(string json)
    {
        if (json.Contains("LineString") && json.Contains("enable"))
            eventDispatcher.Raise(this, new UIEvent() { name = "line string", message = "enter", type = UIEventType.ToolButton });
        if (json.Contains("LineString") && json.Contains("disable"))
            eventDispatcher.Raise(this, new UIEvent() { name = "line string", message = "leave", type = UIEventType.ToolButton });
        Debug.Log("JSBridge recieve Json message: " + json);
    }

    void Start()
    {
        // eventSubscriber = new UIEventSubscriber(eventDispatcher);

    }

    void Update()
    {
        // eventSubscriber.ConsumeAll(EventListener);
    }

    void EventListener(object sender, UIEvent e)
    {
    }
}
