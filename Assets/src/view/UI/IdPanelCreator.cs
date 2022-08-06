using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json.Linq;

public class IdPanelCreator : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    public UIDocument rootUIDocument;
    GameObject IdPanelObj;

    void Start()
    {
        eventDispatcher.eventListener += this.EventListener;

    }

    public void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.PopUp && e.name == "id panel")
        {
            var jsonData = JObject.Parse(e.message);
            string predicate = jsonData["predicate"].Value<string>();
            if (predicate == "hide")
            {
                rootUIDocument.rootVisualElement.Focus();  // prevent warning if we focus on visualElement on IdPanelObj
                Destroy(IdPanelObj);
                IdPanelObj = null;
            }
            else if (predicate == "popup")
            {
                if (IdPanelObj != null)
                {
                    rootUIDocument.rootVisualElement.Focus();  // prevent warning if we focus on visualElement on IdPanelObj
                    Destroy(IdPanelObj);
                    IdPanelObj = null;
                }

                int x = jsonData["x"].Value<int>();
                int y = jsonData["y"].Value<int>();
                string containerId = jsonData["containerId"].Value<string>();
                string childrenId = jsonData["childrenId"].Value<string>();

                IdPanelObj = Instantiate(Resources.Load<GameObject>("UIObj/IdPanel"), this.transform);
                IdPanelObj.GetComponent<IdPanelController>().Init(containerId, childrenId, x, y, eventDispatcher);
            }
        }
    }
}
