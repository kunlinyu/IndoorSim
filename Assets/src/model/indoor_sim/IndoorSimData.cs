using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

#nullable enable

public class IndoorSimData
{

    public IndoorTiling indoorTiling;
    [JsonPropertyAttribute] public IndoorData indoorData = new IndoorData();

    // TODO: put assets and instruction here

    [JsonPropertyAttribute] public List<SimData> simDataList = new List<SimData>();
    public SimData currentSimData;

    public Simulation? simulation = null;



    public IndoorSimData()
    {
        indoorTiling = new IndoorTiling(indoorData, new SimpleIDGenerator("VTX"), new SimpleIDGenerator("BDR"), new SimpleIDGenerator("SPC"));

        SimData simData = new SimData();
        simDataList.Add(simData);
        currentSimData = simData;
    }
}
