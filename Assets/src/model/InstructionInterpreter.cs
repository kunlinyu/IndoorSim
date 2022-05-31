using System;
using System.Linq;
using System.Collections.Generic;

#nullable enable

public interface IInstructionExecutor
{
    public Predicate Predicate();
    public SubjectType Subject();
    public void Execute(ReducedInstruction ins);
}

public class MatcherExecutor : IInstructionExecutor
{
    private Predicate predicate;
    private SubjectType subject;
    private Action<ReducedInstruction> executor;

    public MatcherExecutor(Predicate predicate, SubjectType subject, Action<ReducedInstruction> executor)
    {
        this.predicate = predicate;
        this.subject = subject;
        this.executor = executor;
    }

    public void Execute(ReducedInstruction ins) => executor.Invoke(ins);
    public Predicate Predicate() => predicate;
    public SubjectType Subject() => subject;
}

public class InstructionInterpreter
{
    private List<IInstructionExecutor> executors = new List<IInstructionExecutor>();

    public void RegisterExecutor(IInstructionExecutor exe)
        => executors.Add(exe);

    public void RegisterExecutor(Predicate predicate, SubjectType subject, Action<ReducedInstruction> executor)
    {
        RegisterExecutor(new MatcherExecutor(predicate, subject, executor));
    }

    public void Execute(List<ReducedInstruction> instructions)
    {
        foreach (var instruction in instructions)
            Execute(instruction);
    }

    public void Execute(ReducedInstruction instruction)
    {
        IInstructionExecutor? exe = executors.FirstOrDefault(exe => exe.Predicate() == instruction.predicate && exe.Subject() == instruction.subject);
        if (exe == null)
            throw new InvalidOperationException($"can not find executor match predicate({instruction.predicate}) and subject({instruction.subject})");
        exe.Execute(instruction);
    }

}
