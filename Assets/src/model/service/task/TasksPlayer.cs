using System;
using System.Collections.Generic;

public class TasksPlayer
{
    private List<Task> tasks;
    private List<Task> publishQueue = null;
    private Action<Task> publish;
    private double startTime = 0.0f;
    private int index = 0;

    public TasksPlayer(List<Task> tasks, Action<Task> publish)
    {
        this.tasks = new List<Task>(tasks);
        this.publish = publish;
    }

    public void Reset(double startTime)
    {
        this.startTime = startTime;
        publishQueue = new List<Task>(tasks);
        publishQueue.Sort((t1, t2) => t1.time.CompareTo(t2.time) * -1 );  // The last one is the minimal time one
        index = publishQueue.Count - 1;
        Console.WriteLine($"task player: {publishQueue.Count} task loaded");
    }

    public void TikTok(double currentTime)
    {
        if (publishQueue == null) return;
        while (index >=0 && publishQueue[index].time <= currentTime - startTime)
        {
            publish?.Invoke(publishQueue[index]);
            publishQueue.RemoveAt(index);
            index--;
            Console.WriteLine($"task published. {publishQueue.Count} remained");
        }
    }
}
