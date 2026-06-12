namespace EssSimulator.EssDeviceSimModel.Interface
{
    public interface ITransformerDevice : ITwoPortDevice
    {
        double MagnetizingReactiveKvar { get; }
        double NoLoadActivePowerKw { get; }
    }
}
