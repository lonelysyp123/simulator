namespace EssSimulator.DataExchange.Catalog
{
    /// <summary>
    /// 加法点位绑定：ModelSim 为 model=sum|arg1=&lt;路径A&gt;|arg2=&lt;路径B&gt; 的遥测点，
    /// 寄存器值 = 路径A + 路径B（只读，仅 FC3/4）。
    /// </summary>
    public sealed class SumPointBinding
    {
        /// <summary>ModelSim model 类型标识（不区分大小写）。</summary>
        public const string ModelType = "sum";

        public required MapEntry Entry { get; init; }
        public required string ParamName { get; init; }

        /// <summary>加数 A 的仿真绑定路径（ModelSim arg1）。</summary>
        public required string FirstPath { get; init; }

        /// <summary>加数 B 的仿真绑定路径（ModelSim arg2）。</summary>
        public required string SecondPath { get; init; }

        /// <summary>加法运算：null 或非数值操作数按 0 处理。</summary>
        public static double ComputeSum(object? first, object? second) =>
            ToNumber(first) + ToNumber(second);

        private static double ToNumber(object? value) =>
            value != null && double.TryParse(value.ToString(), out var number) ? number : 0;
    }
}
