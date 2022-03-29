using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SpaceController : MonoBehaviour
{
    private CellSpace space;
    public CellSpace Space
    {
        get { return space; }
        set
        {
            space = value;
        }
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
