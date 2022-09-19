using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

[Serializable]
public enum UIEventType
{
    ToolButton,    // UI -> Scene
    EnterLeaveUIPanel,  // UI -> Scene
    SceneTip,           // Scene -> UI
    UITip,              // Scene -> UI
    Asset,              // Scene -> UI (bidirectional?)
    Hierarchy,          // Scene -> UI
    Simulation,         // UI -> Scene
    PopUp,              // Scene -> UI (bidirectional?)
    IndoorSimData,      // UI -> Scene
    Resources,          // bidirection
    ViewButton,         // UI -> Scene
}

[Serializable]
public struct UIEvent
{
    public string name;
    public string message;
    public string data;
    public UIEventType type;
}

[Serializable]
public class UIEventDispatcher : MonoBehaviour
{
    private ConcurrentQueue<UIEvent> pubEventQueue = new ConcurrentQueue<UIEvent>();
    private List<ConcurrentQueue<UIEvent>> subEventQueue = new List<ConcurrentQueue<UIEvent>>();

    public void Raise(object sender, UIEvent e)
    {
        pubEventQueue.Enqueue(e);
    }

    public ConcurrentQueue<UIEvent> NewSubscribeQueue()
    {
        var newQueue = new ConcurrentQueue<UIEvent>();
        subEventQueue.Add(newQueue);
        return newQueue;
    }

    void Update()
    {
        while (pubEventQueue.TryDequeue(out var evt))
            subEventQueue.ForEach(queue => queue.Enqueue(evt));
    }

}

public class UIEventSubscriber
{
    private UIEventDispatcher dispatcher;
    private ConcurrentQueue<UIEvent> subscribeQueue;

    public UIEventSubscriber(UIEventDispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
        this.subscribeQueue = dispatcher.NewSubscribeQueue();
    }

    public void ConsumeAll(Action<object, UIEvent> listener)
    {
        while (subscribeQueue.TryDequeue(out var evt))
            listener?.Invoke(this, evt);
    }
}
