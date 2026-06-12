namespace EssSimulator.DataExchange.Adapters
{
    public interface ISimulationDataAdapter
    {
        object? Read(string fullBindingPath);
        bool Write(string fullBindingPath, object value);
    }
}
