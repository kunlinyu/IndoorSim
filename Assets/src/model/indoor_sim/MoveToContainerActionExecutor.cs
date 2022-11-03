#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;

public class MoveToContainerActionExecutor : AbstractActionExecutor
{
    readonly MapService map;
    readonly AbstractMotionExecutor moveToCoorMotionExe;


    public MoveToContainerActionExecutor(AbstractMotionExecutor me, MapService map) : base(me)
    {
        this.map = map;
        moveToCoorMotionExe = me;
    }

    public override bool Accept(AgentAction goal)
        => goal.type == ActionType.MoveToContainer;

    public override bool Execute(AgentAction goal, ref bool cancel, out object? result)
    {
        Console.WriteLine("MoveToContainerActionExecutor execute action");
        result = null;
        if (!Accept(goal)) throw new ArgumentException("can not accept the action");

        var action2Container = goal as ActionMoveToContainer ?? throw new ArgumentException("can not cast AgentAction to ActionMoveToContainer");

        Position position;
        if (moveToCoorMotionExe is TranslateToCoorMotionExecutor tl2cME)
            position = tl2cME.Position();
        else if (moveToCoorMotionExe is TwistToCoorMotionExecutor tw2cME)
            position = tw2cME.Position();
        else
            throw new Exception("unknown motion executor type");

        PlanResult? planResult = Plan(position, action2Container.id);
        if (planResult == null)
        {
            Console.WriteLine("plan failed");
            return false;
        }

        PlanSimpleResult simpleResult = planResult.ToSimple();

        List<Motion> motions = new List<Motion>();
        simpleResult.boundaryCentroids.ForEach(p => motions.Add(new MoveToCoorMotion(p.X, p.Y)));

        bool giveUp = false;

        motionExe.SetGoalGroup(motions,
            (motion, result) => { },
            () =>
            {
                giveUp = false;
            },
            (motion, result) =>
            {
                giveUp = true;
                motionExe.AbortRemain();
            });

        while (motionExe.Status() != MotionExecutorStatus.Idle && !giveUp && !cancel)
            Thread.Sleep(10);

        if (cancel)
            motionExe.AbortAll();

        return !giveUp && !cancel;
    }

    private PlanResult? Plan(Position position, string id)
    {
        CoorToContainerQuery query = new CoorToContainerQuery()
        {
            x = position.x,
            y = position.y,
            targetContainerId = id
        };
        return map.Path(query);
    }
}
