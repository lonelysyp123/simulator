namespace EssSimulator.DataExchange.Adapters
{
    public sealed class ReflectionSimulationAdapter : ISimulationDataAdapter
    {
        public object? Read(string fullBindingPath) =>
            SimServer.GetExtIfVariableVal(fullBindingPath);

        public bool Write(string fullBindingPath, object value) =>
            SimServer.SetExtIfVariableVal(fullBindingPath, value);
    }
}
