using System;
using System.Threading;
using System.Threading.Tasks;
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
}

[Serializable]
public struct UIEvent
{
    public string name;
    public string message;
    public UIEventType type;
}

[Serializable]
public class UIEventDispatcher : MonoBehaviour
{
    public delegate void EventListener(object sender, UIEvent e);
    public event EventListener eventListener;
    private ConcurrentQueue<UIEvent> eventQueue = new ConcurrentQueue<UIEvent>();

    public void Raise(object sender, UIEvent e)
    {
        eventQueue.Enqueue(e);
    }

    void Update()
    {
        while (eventQueue.TryDequeue(out var evt))
            eventListener?.Invoke(this, evt);

            // New thread created in Parallel.ForEach cause UI element "can only be called from the main thread." exceptions
            // Parallel.ForEach(eventListener?.GetInvocationList(), inv => inv.DynamicInvoke(this, evt));
    }

}
