using System.Collections.Generic;

namespace EssSimulator.Display
{
public class DpcTestSuite
{
    public List<DpcTestCase> Tests { get; set; } = [];
}

public class DpcTestCase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = [];
    public string Script { get; set; } = string.Empty;
}
}
