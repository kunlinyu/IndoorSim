using System.IO;

#nullable enable

public interface IExporter
{
    public string name { get;  }
    public string defaultStreamName { get; }
    public void Load(IndoorSimData indoorSimData);
    public void Translate();
    public string Export();
    public void Export(Stream stream);
}
