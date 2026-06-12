namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class AcPortSnapshot
    {
        public AcInternalQuantities Internal { get; init; } = new();

        public AcTerminalQuantities Terminal => AcQuantityConverter.ToTerminal(Internal);
    }
}
