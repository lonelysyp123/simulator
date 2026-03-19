using System.Text.Json;

namespace EssSimulator
{
    /// <summary>
    /// 设备信息
    /// @author: syp
    /// @date: 2024/07/05
    /// </summary>
    public class DeviceInfoDto
    {
        public long id { get; set; }

        /// <summary>
        /// 设备名
        /// </summary>
        public string? name { get; set; }

        /// <summary>
        /// 设备类别
        /// </summary>
        public string? type { get; set; }

        /// <summary>
        /// 站点ID
        /// </summary>
        public long siteId { get; set; }

        /// <summary>
        /// 电站ID
        /// </summary>
        public long stationId { get; set; }

        /// <summary>
        /// 产品ID
        /// </summary>
        public long productId { get; set; }

        /// <summary>
        /// 设备序列号
        /// </summary>
        public string? sn { get; set; }

        /// <summary>
        /// 连接类型
        /// </summary>
        public string? connectType { get; set; }

        /// <summary>
        /// 设备ip
        /// </summary>
        public string? ip { get; set; }

        /// <summary>
        ///设备端口号
        /// </summary>
        public int port { get; set; }

        public byte slaveId { get; set; }

        /// <summary>
        /// 采集周期
        /// </summary>
        public int collectionCycle { get; set; }

        public long parentId { get; set; }
        public string? connectParam { get; set; }
    }
}