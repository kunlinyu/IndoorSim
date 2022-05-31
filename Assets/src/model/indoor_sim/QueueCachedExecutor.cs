using System;
using System.Collections.Concurrent;
using System.Threading;

#nullable enable

class GoalCallbacks<GoalType, ResultType>
{
    public GoalType goal;
    public Action<GoalType, ResultType> OnFinish;
    public Action<GoalType, ResultType> OnGiveUp;

    public GoalCallbacks(GoalType goal,
                         Action<GoalType, ResultType> OnFinish,
                         Action<GoalType, ResultType> OnGiveUp)
    {
        this.goal = goal;
        this.OnFinish = OnFinish;
        this.OnGiveUp = OnGiveUp;
    }

}

public class QueuedCachedExecutor<GoalType, ResultType, FeedbackType, StatusType> : IExecutor<GoalType, ResultType, FeedbackType, StatusType>
{
    private IExecutor<GoalType, ResultType, FeedbackType, StatusType> innerExecutor;
    private ConcurrentQueue<GoalCallbacks<GoalType, ResultType>> queue = new ConcurrentQueue<GoalCallbacks<GoalType, ResultType>>();
    private GoalCallbacks<GoalType, ResultType>? currentGoal = null;
    private Thread thread;
    private bool join = false;
    private int sleepMS;

    public QueuedCachedExecutor(IExecutor<GoalType, ResultType, FeedbackType, StatusType> innerExecutor, int sleepMS)
    {
        this.innerExecutor = innerExecutor;
        this.sleepMS = sleepMS;

        thread = new Thread(new ThreadStart(MainLoop));
        thread.Start();

    }

    void MainLoop()
    {
        do
        {
            if (currentGoal == null)
            {
                if (queue.TryDequeue(out currentGoal))
                {
                    innerExecutor.SetGoal(currentGoal.goal,
                        (goal, result) =>
                        {
                            currentGoal.OnFinish?.Invoke(goal, result);
                            currentGoal = null;
                        },
                        (goal, result) =>
                        {
                            currentGoal.OnGiveUp?.Invoke(goal, result);
                            currentGoal = null;
                        });
                }
                else
                {
                    currentGoal = null;
                }
            }
            else
            {
                Thread.Sleep(sleepMS);
            }
        } while (!join);
    }

    void Stop()
    {
        AbortAll();
        join = true;
        thread.Join();
    }

    public StatusType Status() => innerExecutor.Status();

    public bool SetGoal(GoalType goal, Action<GoalType, ResultType> OnFinish, Action<GoalType, ResultType> OnGiveUp)
    {
        if (!Accept(goal)) return false;

        queue.Enqueue(new GoalCallbacks<GoalType, ResultType>(goal, OnFinish, OnGiveUp));
        return true;
    }

    public bool Accept(GoalType goal) => innerExecutor.Accept(goal);

    public void Pause() => innerExecutor.Pause();  // TODO: check race condition
    public void Continue() => innerExecutor.Continue();
    public FeedbackType FeedBack() => innerExecutor.FeedBack();
    public void Abort() => innerExecutor.Abort();

    public void AbortAll()
    {
        queue.Clear();
        innerExecutor.Abort();
        currentGoal = null;
    }

}