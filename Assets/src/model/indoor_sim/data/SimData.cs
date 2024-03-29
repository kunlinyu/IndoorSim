using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class SimData
{
    [JsonProperty] public string name;
    [JsonProperty] public List<AgentDescriptor> agents = new();
    [JsonProperty] public List<Task> tasks = new();
    [JsonProperty] public InstructionHistory<ReducedInstruction> history = new();
    [JsonIgnore] public InstructionInterpreter instructionInterpreter = new();
    [JsonIgnore] public Action<AgentDescriptor> OnAgentCreate = (a) => { };
    [JsonIgnore] public Action<AgentDescriptor> OnAgentRemoved = (a) => { };

    public SimData(string name)
    {
        this.name = name;
        instructionInterpreter.RegisterExecutor(Predicate.Add, SubjectType.Agent, (ins) =>
        {
            Console.WriteLine("execute command add agent");
            AddAgent(ins.newParam.agent().Clone());
        });
        instructionInterpreter.RegisterExecutor(Predicate.Remove, SubjectType.Agent, (ins) =>
        {
            Console.WriteLine("execute command remove agent");
            RemoveAgentEqualsTo(ins.oldParam.agent());
        });
        instructionInterpreter.RegisterExecutor(Predicate.Update, SubjectType.Agent, (ins) =>
        {
            Console.WriteLine("execute command update agent");
            UpdateAgent(ins.oldParam.agent(), ins.newParam.agent());
        });
    }

    public void AddAgent(AgentDescriptor agent)
    {
        agents.Add(agent);
        OnAgentCreate?.Invoke(agent);
    }
    public void RemoveAgentEqualsTo(AgentDescriptor agent)
    {
        int index = agents.FindIndex(a => a.ValueEquals(agent));
        if (index < 0) throw new ArgumentException("can not find the agent to be removed");
        AgentDescriptor goingToRemoved = agents[index];
        agents.RemoveAt(index);
        OnAgentRemoved?.Invoke(goingToRemoved);
    }
    public void RemoveAgent(AgentDescriptor agent)
    {
        bool ret = agents.Remove(agent);
        if (ret == false)
            throw new ArgumentException("can not find the agent to be removed");
        OnAgentRemoved?.Invoke(agent);
    }
    public void UpdateAgent(AgentDescriptor oldAgent, AgentDescriptor newAgent)
    {
        int index = agents.FindIndex(a => a.ValueEquals(oldAgent));
        if (index < 0) throw new ArgumentException("can not find the agent to be updated");
        agents[index].CopyFrom(newAgent);
    }
}
