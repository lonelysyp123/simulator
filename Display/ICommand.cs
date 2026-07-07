namespace EssSimulator.Display
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        CommandResult Execute(string[] args);
    }
}
