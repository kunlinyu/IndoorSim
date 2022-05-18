using System.Collections;
using System.Collections.Generic;

public class IndoorSim
{
    public IndoorTiling indoorTiling;
    public Simulation simulation;

    // TODO: put assets here



    public IndoorSim(IDGenInterface IdGenVertex, IDGenInterface IdGenBoundary, IDGenInterface IdGenSpace)
    {
        indoorTiling = new IndoorTiling(IdGenVertex, IdGenBoundary, IdGenSpace);
    }
}
