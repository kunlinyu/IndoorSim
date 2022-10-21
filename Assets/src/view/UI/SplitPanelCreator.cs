using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SplitPanelCreator : MonoBehaviour
{

    public UIEventDispatcher eventDispatcher;
    private UIEventSubscriber eventSubscriber;
    public UIDocument rootUIDocument;
    GameObject SplitPanelObj = null;

    void Start()
    {
        eventSubscriber = new UIEventSubscriber(eventDispatcher);
    }
    void Update()
    {
        eventSubscriber.ConsumeAll(EventListener);
    }

    public void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.ToolButton && e.name == "split" && e.message == "enter")
        {
            DestroySplitPanel();
            SplitPanelObj = Instantiate(Resources.Load<GameObject>("UIObj/SplitPanel"), this.transform);
            SplitPanelObj.GetComponent<SplitPanelController>().Init(eventDispatcher, "split");
        }

        if (e.type == UIEventType.PopUp && e.name == "split panel" && e.message == "hide")
            DestroySplitPanel();

        if (e.type == UIEventType.ToolButton && e.name == "split" && e.message == "leave")
            DestroySplitPanel();
    }

    private void DestroySplitPanel()
    {
        if (SplitPanelObj != null)
        {
            rootUIDocument.rootVisualElement.Focus();  // prevent warning if we focus on visualElement on IdPanelObj
            Destroy(SplitPanelObj);
            SplitPanelObj = null;
        }
    }
}
