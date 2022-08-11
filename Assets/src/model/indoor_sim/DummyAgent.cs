public class DummyAgent : AbstractAgent
{
    public DummyAgent(AbstractActionPlanner planner, params AbstractActionExecutor[] actionExecutors) : base(planner, actionExecutors)
    {
    }

    public override bool Accept(Task goal) => true;

    public override Capability Capability() => new Capability();
}
