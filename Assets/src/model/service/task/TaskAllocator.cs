using System;
using System.Threading;
using System.Collections.Generic;

#nullable enable

public class TaskAllocator
{
    private List<AbstractAgent> agents;

    private TaskStatusManager? taskStatusManager = null;

    Thread thread;
    bool join = false;


    public TaskAllocator(List<AbstractAgent> agents)
    {
        this.agents = agents;
        thread = new Thread(new ThreadStart(AllocationLoop));
    }

    public void Start()
    {
        taskStatusManager = new TaskStatusManager();
        join = false;
        thread.Start();
    }

    public void AddTask(Task task)
    {
        if (taskStatusManager == null) throw new InvalidOperationException("Start task allocation first");
        taskStatusManager.Add(task);
    }

    void AllocationLoop()
    {
        if (taskStatusManager == null) throw new InvalidOperationException("Start task allocation first");
        while (!join)
        {
            var waitingTasks = taskStatusManager.Tasks(TaskStatus.Waiting);
            if (waitingTasks.Count > 0)
            {
                AbstractAgent? idleAgent = agents.Find(agent => agent.Status() == AgentStatus.Idle);
                if (idleAgent != null)
                {
                    var task = waitingTasks[0];
                    taskStatusManager.Execute(task);
                    idleAgent.SetGoal(task,
                                     (task, result) => { taskStatusManager.Finish(task); },
                                     (task, result) => { taskStatusManager.GiveUp(task); });
                }
            }
            Thread.Sleep(1000);
        }
    }

    public void Stop()
    {
        join = true;
        thread.Join();
        taskStatusManager?.Clear();
        taskStatusManager = null;
    }

}
