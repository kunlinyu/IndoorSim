using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POIController : MonoBehaviour
{
    private IndoorPOI poi;

    public IndoorPOI Poi
    {
        get => poi;
        set
        {
            poi = value;
            poi.OnLocationPointUpdate += UpdateRenderer;
        }
    }

    void UpdateRenderer()
    {

    }

    void Start()
    {

    }

    void Update()
    {

    }
}
