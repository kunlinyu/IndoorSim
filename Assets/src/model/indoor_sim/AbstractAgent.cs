using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

#nullable enable

public enum AgentStatus
{
    Idle,
    Running,
};

public abstract class AbstractAgent : IAsyncExecutor<Task, object?, object?, AgentStatus>, ICapability
{
    private AgentStatus status = AgentStatus.Idle;
    private List<Task> desires = new List<Task>();
    private List<AgentAction> intensions = new List<AgentAction>();

    private Thread? thread;

    private bool join = false;

    private AbstractActionPlanner planner;
    private List<AbstractActionExecutor> actionExecutors;

    public void Reset()
    {
        status = AgentStatus.Idle;
        lock (desires) desires.Clear();
        lock (intensions) intensions.Clear();
        join = true;
        thread?.Join();
        thread = null;
        join = false;
        thread = new Thread(new ThreadStart(MainLoop));
        thread.Start();
    }

    public AbstractAgent(AbstractActionPlanner planner, params AbstractActionExecutor[] actionExecutors)
    {
        this.planner = planner;
        this.actionExecutors = new List<AbstractActionExecutor>(actionExecutors);

        thread = new Thread(new ThreadStart(MainLoop));
        thread.Start();
    }

    private void MainLoop()
    {
        while (!join)
        {
            Task? currentGoal = null;
            lock (desires)
            {
                if (desires.Count > 0)
                {
                    currentGoal = desires[0];
                    status = AgentStatus.Running;
                    desires.RemoveAt(0);
                    Console.WriteLine("agent pick one goal");
                }
                else
                {
                    status = AgentStatus.Idle;
                }
            }

            if (currentGoal != null)
            {
                Console.WriteLine("agent plan");
                intensions.AddRange(planner.Plan(this, currentGoal));
            }
            else
            {
                Thread.Sleep(200);
                continue;
            }

            foreach (var action in intensions)
            {
                var executor = actionExecutors.FirstOrDefault(exe => exe.Accept(action));
                if (executor == null) throw new Exception("no action executor accept this action");
                Console.WriteLine("agent execute action");
                executor.Execute(action, ref join, out var result);
                if (join) break;
            }

            Console.WriteLine("agent finish all action of current goal");
            intensions.Clear();
        }
    }

    public abstract bool Accept(Task goal);

    public void SetGoal(Task goal, Action<Task, object?> OnFinish, Action<Task, object?> OnGiveUp)
    {
        if (!Accept(goal)) throw new ArgumentException("unacceptable task");
        lock (desires)
        {
            desires.Add(goal);
            Console.WriteLine($"agent get task, {desires.Count} remain");
        }
    }

    public abstract Capability Capability();


    public object? FeedBack()
    {
        throw new NotImplementedException();
    }

    public AgentStatus Status() => status;


}