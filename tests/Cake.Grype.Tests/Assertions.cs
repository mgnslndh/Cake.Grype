using Cake.Core;

namespace Cake.Grype.Tests;

internal static class Assertions
{
    public static void IsCakeException(Exception exception, string expectedMessage)
    {
        var cakeException = Assert.IsType<CakeException>(exception);
        Assert.Equal(expectedMessage, cakeException.Message);
    }

    public static void IsArgumentNullException(Exception exception, string expectedParameterName)
    {
        var argumentNullException = Assert.IsType<ArgumentNullException>(exception);
        Assert.Equal(expectedParameterName, argumentNullException.ParamName);
    }

    public static void IsArgumentException(Exception exception, string expectedParameterName)
    {
        var argumentException = Assert.IsType<ArgumentException>(exception);
        Assert.Equal(expectedParameterName, argumentException.ParamName);
    }
}
