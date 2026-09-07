namespace EssSimulator.EssDeviceSimModel.Control
{
    /// <summary>预同步窗口：母线达到门槛后比较待发 V/f/θ 与母线。</summary>
    public sealed class PreSyncSupervisor
    {
        public double EnableVoltagePu { get; }
        public double VoltageWindowPu { get; }
        public double FrequencyWindowHz { get; }
        public double PhaseWindowRad { get; }

        public PreSyncSupervisor(
            double enableVoltagePu = 0.70,
            double voltageWindowPu = 0.05,
            double frequencyWindowHz = 0.2,
            double phaseWindowDeg = 10)
        {
            EnableVoltagePu = Math.Clamp(enableVoltagePu, 0, 1.5);
            VoltageWindowPu = Math.Max(0, voltageWindowPu);
            FrequencyWindowHz = Math.Max(0, frequencyWindowHz);
            PhaseWindowRad = Math.Abs(phaseWindowDeg) * Math.PI / 180.0;
        }

        public bool IsPreSyncActive(double busV, double vNom) =>
            busV >= EnableVoltagePu * Math.Max(vNom, 1.0);

        public bool IsReadyToCutIn(
            double vOwn,
            double fOwn,
            double thetaOwn,
            double vBus,
            double fBus,
            double thetaBus,
            double vNom)
        {
            if (!IsPreSyncActive(vBus, vNom))
                return false;

            double vn = Math.Max(vNom, 1.0);
            if (Math.Abs(vOwn - vBus) / vn > VoltageWindowPu)
                return false;
            if (Math.Abs(fOwn - fBus) > FrequencyWindowHz)
                return false;
            return Math.Abs(PhaseIntegrator.AngleErrorRad(thetaOwn, thetaBus)) <= PhaseWindowRad;
        }
    }
}
