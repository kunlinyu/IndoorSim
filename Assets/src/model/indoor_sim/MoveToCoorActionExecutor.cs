using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MoveToCoorActionExecutor : AbstractActionExecutor
{
    public MoveToCoorActionExecutor(AbstractMotionExecutor me) : base(me)
    { }

    public override bool Accept(AgentAction goal)
        => goal.type == ActionType.MoveToCoor;

    public override bool Execute(AgentAction goal, ref bool cancel, out object result)
    {
        var action2Coor = goal as ActionMoveToCoor ?? throw new ArgumentException("can not cast AgentAction to ActionMoveToCoor");

        bool giveUp = false;
        motionExe.SetGoalGroup(new List<Motion>() { new MoveToCoorMotion(action2Coor.x, action2Coor.y) },
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

        result = null;
        return !giveUp && !cancel;
    }
}
