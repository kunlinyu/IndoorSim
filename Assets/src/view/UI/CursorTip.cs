using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CursorTip : MonoBehaviour
{
    [SerializeField] public UIEventDispatcher eventDispatcher;
    private string uiMessage;
    private string sceneMessage;
    private bool MouseOnUI;

    void Start()
    {
        Label tip = GetComponent<UIDocument>().rootVisualElement.Q<Label>("Tip");
        tip.text = "xxx";

        eventDispatcher.eventListener += EventListener;
    }

    void Update()
    {
        Label tip = GetComponent<UIDocument>().rootVisualElement.Q<Label>("Tip");
        if (MouseOnUI)
            tip.text = uiMessage;
        else
            tip.text = sceneMessage;
        tip.style.left = Input.mousePosition.x;
        tip.style.bottom = Input.mousePosition.y;
    }

    void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.UITip)
        {
            uiMessage = e.message;
        }
        else if (e.type == UIEventType.SceneTip)
        {
            sceneMessage = e.message;
        }
        else if (e.type == UIEventType.EnterLeaveUIPanel)
        {
            if (e.message == "enter")
                MouseOnUI = true;
            if (e.message == "leave")
                MouseOnUI = false;
        }
    }
}
