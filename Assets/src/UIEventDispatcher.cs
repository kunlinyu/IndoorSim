using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum UIEventType
{
    ButtonClick,
    EnterUIPanel,
    Tip,
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

    public void Raise(object sender, UIEvent e)
    {
        lock (this)
        {
            eventListener?.Invoke(sender, e);
        }
    }

}
