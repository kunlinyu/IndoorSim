using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class Move2AgentModel : MonoBehaviour, IActuatorSensor
{
    public AgentDescriptor AgentDescriptor { get; set; }
    private Action<ISensorData> listener;
    private SpeedVec speed = new SpeedVec() { x = 0.0f, y = 0.0f };
    private bool reset = false;

    public AbstractMotionExecutor? motionExecutor { get; set; }


    void FixedUpdate()
    {
        listener?.Invoke(new Position() { x = transform.position.x, y = transform.position.z });

        lock (speed)
        {
            Vector3 position = transform.position;
            position.x += (float)speed.x * Time.deltaTime;
            position.z += (float)speed.y * Time.deltaTime;
            transform.position = position;
        }

        if (reset)
        {
            reset = false;
            transform.position = new Vector3(AgentDescriptor.x, 0.0f, AgentDescriptor.y);
            transform.rotation = Quaternion.Euler(0.0f, AgentDescriptor.theta, 0.0f);
        }
    }

    void Update()
    {
        var speedLR = transform.Find("AgentSpeedLineRenderer").gameObject.GetComponent<LineRenderer>();
        speedLR.positionCount = 2;
        speedLR.SetPosition(0, transform.position);
        lock (speed)
        {
            Vector3 speedTarget = transform.position + new Vector3((float)speed.x, 0.0f, (float)speed.y);
            speedLR.SetPosition(1, speedTarget);
        }

        var motionLR = transform.Find("AgentMotionLineRenderer").gameObject.GetComponent<LineRenderer>();
        if (motionExecutor != null && motionExecutor.currentGoal != null)
        {
            if (motionExecutor.currentGoal.type == MotionType.Move)
            {
                motionLR.positionCount = 2;
                motionLR.SetPosition(0, transform.position);
                var moveToCoor = motionExecutor.currentGoal as MoveToCoorMotion;
                if (moveToCoor == null) throw new Exception("can not cast to MoveToCoorMotion");
                Vector3 target = new Vector3((float)moveToCoor.x, 0.0f, (float)moveToCoor.y);
                motionLR.SetPosition(1, target);
            }
            else
            {
                throw new Exception("unknown motion type: " + motionExecutor.currentGoal.type);
            }
        }
        else
        {
            motionLR.positionCount = 0;
        }

    }

    public void RegisterSensorDataListener(Action<ISensorData> listener)
    {
        this.listener += listener;
    }

    public void Execute(IActuatorCommand command)
    {
        lock (speed)
            speed = command as SpeedVec ?? throw new ArgumentException("accept only speed vector command");
    }

    public void ResetToInitStatus()
    {
        reset = true;
    }
}
