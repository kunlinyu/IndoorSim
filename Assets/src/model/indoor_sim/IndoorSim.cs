using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndoorSim
{
    [SerializeField] public IndoorTiling indoorTiling;

    public IndoorSim(IDGenInterface IdGenVertex, IDGenInterface IdGenBoundary, IDGenInterface IdGenSpace)
    {
        indoorTiling = new IndoorTiling(IdGenVertex, IdGenBoundary, IdGenSpace);
    }
}
