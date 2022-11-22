using System.IO;

public class LinuxConfigStorage : IConfigStorage
{
    public string Load(string name)
    {
        return File.ReadAllText("~/.IndoorSim/" + name);
    }

    public void Save(string name, string data)
    {
        File.WriteAllText("~/.IndoorSim/" + name, data);
    }
}
