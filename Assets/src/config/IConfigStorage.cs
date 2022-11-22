public interface IConfigStorage
{
    string Load(string name);
    void Save(string name, string data);
}
