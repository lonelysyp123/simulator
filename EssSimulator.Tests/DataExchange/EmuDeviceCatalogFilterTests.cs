using EssSimulator.Configuration;
using EssSimulator.DataExchange.Catalog;

namespace EssSimulator.Tests.DataExchange;

/// <summary>EMU 点表绑定按机组构成门控：只能指向机组内实际存在的设备模型。</summary>
public class EmuDeviceCatalogFilterTests
{
    private static EssUnitConfig Unit(int pcsCount, bool breaker, bool meter)
    {
        var u = new EssUnitConfig { HasUnitBreaker = breaker, HasUnitMeter = meter };
        for (int i = 0; i < pcsCount; i++)
            u.Pcs.Add(new PcsDeviceConfig());
        return u;
    }

    [Fact]
    public void Create_ActivatesOnlyForEmuLikeDevicesWithUnits()
    {
        var units = new[] { Unit(2, true, true) };
        Assert.NotNull(EmuDeviceCatalogFilter.Create("simEmu1", units));
        Assert.NotNull(EmuDeviceCatalogFilter.Create("simLc1", units));
        // 非 EMU 同构设备不门控
        Assert.Null(EmuDeviceCatalogFilter.Create("simBms1", units));
        Assert.Null(EmuDeviceCatalogFilter.Create("simEm", units));
        // legacy 配置（无机组构成）不门控，保持现状
        Assert.Null(EmuDeviceCatalogFilter.Create("simEmu1", null));
        Assert.Null(EmuDeviceCatalogFilter.Create("simEmu1", Array.Empty<EssUnitConfig>()));
    }

    [Fact]
    public void Allows_GatesPathsByUnitComposition()
    {
        var filter = EmuDeviceCatalogFilter.Create("simEmu1", new[]
        {
            Unit(pcsCount: 2, breaker: true, meter: false),
            Unit(pcsCount: 1, breaker: false, meter: true)
        })!;

        // 根必须是 emuK 机组根路径
        Assert.False(filter.Allows("bms1.Soc"));
        Assert.False(filter.Allows("emu3.PcsList[0].X"));
        Assert.False(filter.Allows(null));

        // PcsList[i] 要求 i 小于该机组 PCS 台数
        Assert.True(filter.Allows("emu1.PcsList[0].pcsOnOffSwitch"));
        Assert.True(filter.Allows("emu1.PcsList[1].pcsOnOffSwitch"));
        Assert.False(filter.Allows("emu1.PcsList[2].pcsOnOffSwitch"));
        Assert.False(filter.Allows("emu2.PcsList[1].pcsOnOffSwitch"));

        // ElectricityMeter 要求该机组绑定电表
        Assert.False(filter.Allows("emu1.ElectricityMeter.Uab"));
        Assert.True(filter.Allows("emu2.ElectricityMeter.Uab"));

        // Emu.PowerOnOff（单元高压断路器开合）要求该机组绑定断路器
        Assert.True(filter.Allows("emu1.Emu.PowerOnOff"));
        Assert.False(filter.Allows("emu2.Emu.PowerOnOff"));

        // 其余 Emu.* 为单元虚拟模型，恒允许
        Assert.True(filter.Allows("emu2.Emu.TargetActivePower"));
        Assert.True(filter.Allows("emu1.Emu.SystemOperation"));
    }

    [Fact]
    public void FilterPaths_DropsWholePointWhenFewerThanTwoPathsRemain()
    {
        var filter = EmuDeviceCatalogFilter.Create("simEmu1", new[] { Unit(1, breaker: true, meter: false) })!;

        // 过滤后仅剩 1 条：sum/max 非法绑定，整点剔除
        Assert.Null(filter.FilterPaths(new[] { "emu1.PcsList[0].P", "emu1.PcsList[1].P" }));
        // 全部无效：整点剔除
        Assert.Null(filter.FilterPaths(new[] { "emu1.ElectricityMeter.A", "emu1.ElectricityMeter.B" }));
        // 保留两条及以上有效路径
        var kept = filter.FilterPaths(new[]
        {
            "emu1.PcsList[0].ActivePower",
            "emu1.PcsList[1].ActivePower",
            "emu1.Emu.TargetActivePower"
        });
        Assert.NotNull(kept);
        Assert.Equal(new[] { "emu1.PcsList[0].ActivePower", "emu1.Emu.TargetActivePower" }, kept);
    }
}
