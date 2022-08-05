using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CursorTip : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    public UIDocument rootUIDocument;

    Label tip;
    private string uiMessage;
    private string sceneMessage;
    private bool MouseOnUI;

    void Start()
    {
        this.tip = rootUIDocument.rootVisualElement.Q<Label>("Tip");
        this.tip.text = "";
        eventDispatcher.eventListener += this.EventListener;
    }

    void Update()
    {
        if (MouseOnUI)
            tip.text = uiMessage;
        else
            tip.text = sceneMessage;
        tip.style.left = Input.mousePosition.x;
        tip.style.bottom = Input.mousePosition.y;
    }

    public void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.UITip)
            uiMessage = e.message;
        else if (e.type == UIEventType.SceneTip)
            sceneMessage = e.message;
        else if (e.type == UIEventType.EnterLeaveUIPanel)
        {
            if (e.message == "enter")
                MouseOnUI = true;
            if (e.message == "leave")
                MouseOnUI = false;
        }
    }
}
