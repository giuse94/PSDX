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
        var ex = Assert.Throws<ArgumentException>(() => new CrashBandicoot2SaveData(fs));
        Assert.StartsWith("Only the European version of Crash Bandicoot 2", ex.Message, StringComparison.InvariantCultureIgnoreCase);
    }
}
