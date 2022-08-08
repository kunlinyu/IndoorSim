using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaAmrPoiController : MonoBehaviour
{
    private PaAmrPoi paAmrPoi;

    public PaAmrPoi PaAmrPoi
    {
        get => paAmrPoi;
        set
        {
            paAmrPoi = value;
            // paAmrPoi.OnUpdate += ReRender;
        }
    }

    void ReRender()
    {

    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
