using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class SimData
{
    [JsonProperty] public string name;
    [JsonProperty] public bool active;
    [JsonProperty] public List<AgentDescriptor> agents = new List<AgentDescriptor>();
    [JsonProperty] public List<Task> tasks = new List<Task>();
    [JsonProperty] public InstructionHistory<ReducedInstruction> history = new InstructionHistory<ReducedInstruction>();
    [JsonIgnore] public InstructionInterpreter instructionInterpreter = new InstructionInterpreter();

    public SimData(string name)
    {
        this.name = name;
        instructionInterpreter.RegisterExecutor(Predicate.Add, SubjectType.Agent, (ins) =>
        {
            agents.Add(ins.newParam.agent().Clone());
        });
        instructionInterpreter.RegisterExecutor(Predicate.Remove, SubjectType.Agent, (ins) =>
        {
            int index = agents.FindIndex(agent => agent.Equals(ins.oldParam.agent()));
            if (index < 0) throw new ArgumentException("can not find the agent to be removed");
            agents.RemoveAt(index);
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.Agent, (ins) =>
        {
            int index = agents.FindIndex(agent => agent.Equals(ins.oldParam.agent()));
            if (index < 0) throw new ArgumentException("can not find the agent to be updated");
            agents[index] = ins.newParam.agent().Clone();
        });
    }
}
