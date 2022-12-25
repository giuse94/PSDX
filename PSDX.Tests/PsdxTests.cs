using Xunit;

namespace PSDX.Tests;

public class PsdxTests
{
    [Fact]
    public void PsxSaveDataCtorThrowsAneWithNullStream()
    {
        static void CodeToTest()
        {
            Stream? nullStream = null;
            _ = new PsxSaveData(nullStream!); // Intentionally suppress the (right) warning.
        }

        Assert.Throws<ArgumentNullException>(CodeToTest);
    }
}