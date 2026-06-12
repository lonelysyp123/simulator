namespace EssSimulator.Display
{
public interface ICommand
{
    string Name { get; }
    string Description { get; }
    void Execute(string[] args);
}
}
