using IEC61850_simulatorServer2.EssDeviceSimModel;
using IEC61850_simulatorServer2.EssSimModelApi;
using IEC61850_simulatorServer2.Display;
using log4net;
using log4net.Config;


namespace IEC61850_simulatorServer2
{
     
    class MainClass
    {
        public static void Main(string[] args)
        {
            // 尝试加载 log4net.config（优先应用目录，回退当前目录）并写入启动日志
            try
            {
                var baseConfig = Path.Combine(AppContext.BaseDirectory, "log4net.config");
                if (File.Exists(baseConfig))
                {
                    XmlConfigurator.Configure(new FileInfo(baseConfig));
                }
                else if (File.Exists("log4net.config"))
                {
                    XmlConfigurator.Configure(new FileInfo("log4net.config"));
                }
            }
            catch (Exception ex)
            {
                // 配置加载失败：回退到基础配置，并打印提示
                BasicConfigurator.Configure();
                Console.WriteLine($"[log4net] 配置加载失败：{ex.Message}，已启用基础配置");
            }
            LogManager.GetLogger(typeof(MainClass)).Info("[Program] 应用启动，log4net 配置已尝试加载");

            // program now only supports Modbus. parse minimal args --bmsmap and --modbusport
            int clusterCount = 12;
            int packCount = 4;
            int modbusPort = 1502;
            bool isNoGUI = false;

            // 储能单元仿真模型（保持原有 ModelSim 功能，不做修改）
            EnergyStorageSystem eSS = new EnergyStorageSystem(null, clusterCount, packCount);

            //把ess模拟器添加到对象管理器（保持 ModelSim 行为）
            var objectsCollect = ObjectsCollect.Instance;
            objectsCollect.AddObjects("ess", eSS);

            int unitCount = 2;
            //根据单元数量启动对应数量的 Modbus 模拟服务器
            for (int i = 0; i < unitCount; i++)
            {
                int unitModbusPort = modbusPort + i * 10; // 每个单元间隔10端口
                
                // BMS协议模拟器
                string bmsName = $"simBms{i + 1}";
                ModbusSimServer modbusSimServer = new ModbusSimServer("bms_bank.csv", unitModbusPort, bmsName, clusterCount);
                objectsCollect.AddObjects(bmsName, modbusSimServer);
                modbusSimServer.Start();
                SimServer.serverListenInfo[$"{bmsName}"] = $"Modbus TCP 端口 {unitModbusPort}";
            }

            // PCS协议模拟器
            string pcsName = $"simEmu";
            ModbusSimServer modbus_emu = new ModbusSimServer("emu.csv", modbusPort - 1, pcsName);
            objectsCollect.AddObjects(pcsName, modbus_emu);
            modbus_emu.Start();
            SimServer.serverListenInfo[$"{pcsName}"] = $"Modbus TCP 端口 {modbusPort - 1}";

            // 电表协议模拟器
            string emName = $"simEm";
            ModbusSimServer modbus_em = new ModbusSimServer("em.csv", modbusPort - 2, emName);
            objectsCollect.AddObjects(emName, modbus_em);
            modbus_em.Start();
            SimServer.serverListenInfo[$"{emName}"] = $"Modbus TCP 端口 {modbusPort - 2}";

            Thread.Sleep(1000);

            // 启动 BMS、PCS 接口数据服务器（保持原有）
            BmsDataService bmsDataService = new BmsDataService(clusterCount, packCount);
            PcsDataServer pcsDataServer = new PcsDataServer();
            EmDataService emDataService = new EmDataService();

            if (!isNoGUI)
            {
                GuiMain gui = new GuiMain();
            }

        }
    }
}