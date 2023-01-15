using System;
using System.IO;
using Xunit;

namespace PSDX.Tests;

public class PsdxTests
{
    [Fact]
    public void PsxSaveDataCtorThrowsAneWithNullStream()
    {
        static void CodeToTest() => _ = new PsxSaveData(null!); // Intentionally suppress the (right) warning.

        _ = Assert.Throws<ArgumentNullException>(CodeToTest);
    }

    [Fact]
    public void PsxSaveDataCtorThrowsAeWithWrongStream()
    {
        static void CodeToTest() => _ = new PsxSaveData(new MemoryStream());

        _ = Assert.Throws<ArgumentException>(CodeToTest);
    }

    [Fact]
    public void Cb2SaveDataCtorThrowsAneWithNullStream()
    {
        static void CodeToTest() => _ = new CrashBandicoot2SaveData(null!); // Intentionally suppress the (right) warning.

        _ = Assert.Throws<ArgumentNullException>(CodeToTest);
    }

    [Fact]
    public void Cb2SaveDataCtorThrowsAeWithWrongStream()
    {
        static void CodeToTest() => _ = new CrashBandicoot2SaveData(new MemoryStream());

        var ex = Assert.Throws<ArgumentException>(CodeToTest);
        Assert.StartsWith("The size of Single Save Format files", ex.Message, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public void Cb2SaveDataCtorThrowsAeWithWrongFile()
    {
        using var fs = new FileStream("cb3.mcs", FileMode.Open);

        void CodeToTest() => _ = new CrashBandicoot2SaveData(fs);

        var ex = Assert.Throws<ArgumentException>(CodeToTest);
        Assert.StartsWith("Only the European version of Crash Bandicoot 2", ex.Message, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public void GetAkuAkuMasksReturns2()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        int masks = cb2.GetAkuAkuMasks();

        Assert.Equal(2, masks);
    }

    [Theory]
    [InlineData(-1), InlineData(0), InlineData(1), InlineData(2), InlineData(3), InlineData(4)]
    public void TestSetAkuAkuMasks(int masksToSet)
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        cb2.SetAkuAkuMasks(masksToSet);

        Assert.Equal(masksToSet, cb2.GetAkuAkuMasks());
    }
}
