namespace Cake.Grype.Tests;

public sealed class GrypeSeverityTests
{
    [Fact]
    public void Should_Be_Ordered_Like_Grype()
    {
        // grype/vulnerability/severity.go: UnknownSeverity = iota, Negligible, Low, Medium, High, Critical
        Assert.Equal(0, (int)GrypeSeverity.Unknown);
        Assert.Equal(1, (int)GrypeSeverity.Negligible);
        Assert.Equal(2, (int)GrypeSeverity.Low);
        Assert.Equal(3, (int)GrypeSeverity.Medium);
        Assert.Equal(4, (int)GrypeSeverity.High);
        Assert.Equal(5, (int)GrypeSeverity.Critical);
    }

    [Fact]
    public void Should_Rank_Unknown_Below_Every_Assessed_Severity()
    {
        Assert.True(GrypeSeverity.Unknown < GrypeSeverity.Negligible);
        Assert.True(GrypeSeverity.Critical >= GrypeSeverity.High);
    }
}
