using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class SimData
{
    [JsonPropertyAttribute] public List<AgentDescriptor> agents;
    [JsonPropertyAttribute] public List<Task> tasks;
}
