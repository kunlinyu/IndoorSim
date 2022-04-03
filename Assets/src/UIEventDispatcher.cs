using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UIEventType
{
    ButtonClick,
    EnterUIPanel,
}

public struct UIEvent
{
    public string name;
    public UIEventType type;
}

public class UIEventDispatcher
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
