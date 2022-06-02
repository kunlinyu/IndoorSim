using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

#nullable enable

class GoalCallbacks<GoalType, ResultType>
{
    public GoalType goal;
    public Action<GoalType, ResultType>? OnFinish;
    public Action<GoalType, ResultType>? OnGiveUp;

    public GoalCallbacks(GoalType goal,
                         Action<GoalType, ResultType>? OnFinish,
                         Action<GoalType, ResultType>? OnGiveUp)
    {
        this.goal = goal;
        this.OnFinish = OnFinish;
        this.OnGiveUp = OnGiveUp;
    }

}

public class QueuedCachedExecutor<GoalType, ResultType, FeedbackType, StatusType> : IExecutor<GoalType, ResultType, FeedbackType, StatusType>
{
    private IExecutor<GoalType, ResultType, FeedbackType, StatusType> innerExecutor;
    private BlockingCollection<GoalCallbacks<GoalType, ResultType>> queue;
    private CancellationTokenSource source = new CancellationTokenSource();
    private Thread thread;

    private Dictionary<GoalType, ResultType> blockExecuteFinishResult = new Dictionary<GoalType, ResultType>();
    private Dictionary<GoalType, ResultType> blockExecuteGiveUpResult = new Dictionary<GoalType, ResultType>();

    public QueuedCachedExecutor(IExecutor<GoalType, ResultType, FeedbackType, StatusType> innerExecutor)
    {
        this.innerExecutor = innerExecutor;
        queue = new BlockingCollection<GoalCallbacks<GoalType, ResultType>>(new ConcurrentQueue<GoalCallbacks<GoalType, ResultType>>());

        thread = new Thread(new ThreadStart(MainLoop));
        thread.Start();

    }

    void MainLoop()
    {
        while (true)
        {
            try
            {
                var currentGoal = queue.Take(source.Token);
                bool finish = innerExecutor.Execute(currentGoal.goal, out var result);

                if (finish)
                    currentGoal.OnFinish?.Invoke(currentGoal.goal, result);
                else
                    currentGoal.OnGiveUp?.Invoke(currentGoal.goal, result);

            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    void Stop()
    {
        source.Cancel();
        AbortAll();
        thread.Join();
    }

    public StatusType Status() => innerExecutor.Status();

    public void SetGoal(GoalType goal, Action<GoalType, ResultType> OnFinish, Action<GoalType, ResultType> OnGiveUp)
    {
        if (!Accept(goal)) throw new ArgumentException("we don't accept the goal");
        queue.Add(new GoalCallbacks<GoalType, ResultType>(goal, OnFinish, OnGiveUp));
    }

    public bool Accept(GoalType goal) => innerExecutor.Accept(goal);

    public void Pause() => innerExecutor.Pause();  // TODO: check race condition
    public void Continue() => innerExecutor.Continue();
    public FeedbackType FeedBack() => innerExecutor.FeedBack();
    public void Abort() => innerExecutor.Abort();

    public void AbortAll()
    {
        queue.Dispose();
        innerExecutor.Abort();
    }

    public bool Execute(GoalType goal, out ResultType result)
    {
        if (!Accept(goal)) throw new ArgumentException("we don't accept the goal");

        queue.Add(new GoalCallbacks<GoalType, ResultType>(goal,
            (goal, result) => { blockExecuteFinishResult.Add(goal, result); },
            (goal, result) => { blockExecuteGiveUpResult.Add(goal, result); }));

        bool giveUp = false;
        while (true)
        {
            Thread.Sleep(20);

            if (blockExecuteFinishResult.ContainsKey(goal))
            {
                blockExecuteFinishResult.Remove(goal, out result);
                giveUp = false;
                break;
            }
            if (blockExecuteGiveUpResult.ContainsKey(goal))
            {
                blockExecuteGiveUpResult.Remove(goal, out result);
                giveUp = true;
                break;
            }
        }

        return !giveUp;
    }
}