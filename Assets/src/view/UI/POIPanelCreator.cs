using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class POIPanelCreator : MonoBehaviour
{

    public UIEventDispatcher eventDispatcher;
    private UIEventSubscriber eventSubscriber;
    public UIDocument rootUIDocument;
    GameObject POIPanelObj = null;
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
        if (e.type == UIEventType.ToolButton && e.name == "paamrpoi" && e.message == "enter")
        {
            DestroyPOIPanel();
            POIPanelObj = Instantiate(Resources.Load<GameObject>("UIObj/POIPanel"), this.transform);
            POIPanelObj.GetComponent<POIPanelController>().Init(eventDispatcher, "paamrpoi");
        }

        if (e.type == UIEventType.PopUp && e.name == "poi panel" && e.message == "hide")
            DestroyPOIPanel();

        if (e.type == UIEventType.ToolButton && e.name == "paamrpoi" && e.message == "leave")
            DestroyPOIPanel();
    }

    private void DestroyPOIPanel()
    {
        if (POIPanelObj != null)
        {
            rootUIDocument.rootVisualElement.Focus();  // prevent warning if we focus on visualElement on IdPanelObj
            Destroy(POIPanelObj);
            POIPanelObj = null;
        }
    }
}
