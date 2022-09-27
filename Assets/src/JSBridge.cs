using System.Collections.Concurrent;
using UnityEngine;
using System.Runtime.InteropServices;

struct JsRequest
{
    public int number;
    public string json;
}

public class JSBridge : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    // private UIEventSubscriber eventSubscriber;
    [DllImport("__Internal")]
    private static extern void Response(int number, string str);

    private ConcurrentQueue<JsRequest> jsRequests = new ConcurrentQueue<JsRequest>();

    void SendJsonMessage(string json)
    {
        if (json.Contains("LineString") && json.Contains("enable"))
            eventDispatcher.Raise(this, new UIEvent() { name = "line string", message = "enter", type = UIEventType.ToolButton });
        if (json.Contains("LineString") && json.Contains("disable"))
            eventDispatcher.Raise(this, new UIEvent() { name = "line string", message = "leave", type = UIEventType.ToolButton });
        Debug.Log("JSBridge recieve Json message: " + json);
    }

    void Request(string mixNumberJson)
    {
        string delimiter = "@@@";
        int length = mixNumberJson.IndexOf(delimiter);
        int number = int.Parse(mixNumberJson.Substring(0, length));
        string json = mixNumberJson.Substring(length + delimiter.Length);

        Debug.Log($"C# get Request with number {number}, json {json}");
        jsRequests.Enqueue(new JsRequest() { number = number, json = json });
    }

    void Start()
    {
        // eventSubscriber = new UIEventSubscriber(eventDispatcher);
    }

    void Update()
    {
        // eventSubscriber.ConsumeAll(EventListener);

        while (jsRequests.Count > 0)
        {
            bool ret = jsRequests.TryDequeue(out var jsRequest);
            if (ret)
            {
                string response = "response of request(" + jsRequest.json + ")";
                Response(jsRequest.number, response);
            }
        }
    }

    void EventListener(object sender, UIEvent e)
    {
    }
}
