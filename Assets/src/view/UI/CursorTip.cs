using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CursorTip : MonoBehaviour
{
    [SerializeField] public UIEventDispatcher eventDispatcher;

    private string message;

    void Start()
    {
        Label tip = GetComponent<UIDocument>().rootVisualElement.Q<Label>("Tip");
        tip.text = "xxx";
        // tip.visible = false;

        eventDispatcher.eventListener += EventListener;
    }

    void Update()
    {
        Label tip = GetComponent<UIDocument>().rootVisualElement.Q<Label>("Tip");
        tip.text = message;
        tip.style.left = Input.mousePosition.x;
        tip.style.bottom = Input.mousePosition.y;
    }

    void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.Tip)
            message = e.message;
    }
}
