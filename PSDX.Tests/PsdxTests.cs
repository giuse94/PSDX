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

    [Fact]
    public void TestGetChecksum()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        uint checksum = cb2.GetChecksum();

        Assert.Equal(0xE2BC35B9, checksum);
    }

    [Theory]
    [InlineData(0), InlineData(7), InlineData(0xA14DC582), InlineData(uint.MaxValue)]
    public void TestSetChecksum(uint checksumToSet)
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        cb2.SetChecksum(checksumToSet);

        Assert.Equal(checksumToSet, cb2.GetChecksum());
    }

    [Fact]
    public void TestComputeChecksum()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        uint checksum = cb2.ComputeChecksum();

        Assert.Equal(0xE2BC35B9, checksum);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public void TestGetStreamBehavior(bool computeRightChecksum)
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs) { ComputeRightChecksum = computeRightChecksum };
        const uint randomChecksum = 491;
        cb2.SetChecksum(randomChecksum);

        _ = cb2.GetStream();
        uint storedChecksum = cb2.GetChecksum();

        if (computeRightChecksum)
        {
            Assert.NotEqual(randomChecksum, storedChecksum);
        }
        else
        {
            Assert.Equal(randomChecksum, storedChecksum);
        }
    }

    [Fact]
    public void CallersCantInterfereWithStream()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);
        var stream = cb2.GetStream();

        stream.Position = 0;
        byte[] bytes = new byte[stream.Length];
        stream.Write(bytes);

        Assert.Equal(2, cb2.GetAkuAkuMasks());
        Assert.Equal(0xE2BC35B9, cb2.GetChecksum());
    }
}
