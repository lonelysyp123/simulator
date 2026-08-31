using EssSimulator.LocalControl;

namespace EssSimulator.Tests.LocalControl;

public class LcRuntimeFactoryTests
{
    [Theory]
    [InlineData(null, typeof(StandardLcRuntime))]
    [InlineData("", typeof(StandardLcRuntime))]
    [InlineData("standard", typeof(StandardLcRuntime))]
    [InlineData("unknown-model", typeof(StandardLcRuntime))]
    [InlineData("trina_5.5MW", typeof(Trina55MwLcRuntime))]
    [InlineData("trina_10MW", typeof(Trina10MwLcRuntime))]
    public void Create_MapsLcModelIdToRuntimeType(string? modelId, Type expected)
    {
        var runtime = LcRuntimeFactory.Create(modelId);
        Assert.IsType(expected, runtime);
    }

    [Fact]
    public void TrinaRuntimes_AreModelBound_NotStandardBridge()
    {
        var t55 = LcRuntimeFactory.Create("trina_5.5MW");
        var t10 = LcRuntimeFactory.Create("trina_10MW");

        Assert.IsAssignableFrom<ModelBoundLcRuntime>(t55);
        Assert.IsAssignableFrom<ModelBoundLcRuntime>(t10);
        Assert.IsNotType<StandardLcRuntime>(t55);
        Assert.IsNotType<StandardLcRuntime>(t10);
    }
}
