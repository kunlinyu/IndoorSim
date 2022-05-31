using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;

#nullable enable

public enum AgentStatus
{
    Idle,
    Running,
};

public interface IAgent : IExecutor<Task, object, Task?, AgentStatus>, ICapability
{

}