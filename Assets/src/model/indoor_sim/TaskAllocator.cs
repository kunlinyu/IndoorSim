using System;
using System.Threading;
using System.Collections.Generic;

#nullable enable

public class TaskAllocator
{
    private List<IAgent> agents;
    private Queue<Task>? taskQueue = null;

    Thread thread;
    bool join = false;


    public TaskAllocator(List<IAgent> agents)
    {
        this.agents = agents;
        thread = new Thread(new ThreadStart(AllocationLoop));
    }

    public void Start()
    {
        taskQueue = new Queue<Task>();
        join = false;
        thread.Start();
    }

    public void AddTask(Task task)
    {
        if (taskQueue == null) throw new InvalidOperationException("Start task allocation first");
        lock (taskQueue)
        {
            taskQueue.Enqueue(task);
        }
    }

    void AllocationLoop()
    {
        if (taskQueue == null) throw new InvalidOperationException("Start task allocation first");
        while (!join)
        {
            lock (taskQueue)
            {
                if (taskQueue.Count > 0)
                {
                    IAgent? idleAgent = agents.Find(agent => agent.Status() == AgentStatus.Idle);
                    if (idleAgent != null)
                    {
                        var task = taskQueue.Dequeue();
                        idleAgent.SetGoal(task, (task, result) => { }, (task, result) => { });
                    }
                }
            }
            Thread.Sleep(10);
        }
    }

    public void Stop()
    {
        join = true;
        thread.Join();
        taskQueue?.Clear();
        taskQueue = null;
    }

}
