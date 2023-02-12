using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
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

    [Fact]
    public void OriginalDataIsNotChanged()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var ms = new MemoryStream();
        fs.CopyTo(ms);
        byte[] originalData = ms.ToArray();

        var cb2 = new CrashBandicoot2SaveData(fs);
        cb2.SetAkuAkuMasks(1);
        cb2.SetChecksum(515);

        ms = new MemoryStream();
        fs.Position = 0;
        fs.CopyTo(ms);
        Assert.True(originalData.SequenceEqual(ms.ToArray()));
    }

    [Theory]
    [InlineData(-1), InlineData(0), InlineData(28)]
    public void LevelRelatedMethodsThrowAoReWithWrongNumber(int level)
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        void GetProgressStatus() => cb2.GetProgressStatus(level);
        void SetProgressStatus() => cb2.SetProgressStatus(level, true);
        void GetCrystalStatus() => cb2.GetCrystalStatus(level);
        void SetCrystalStatus() => cb2.SetCrystalStatus(level, true);
        void GetGemStatus() => cb2.GetGemStatus(level, CrashBandicoot2SaveData.GemType.AllBoxesGem);
        void SetGemStatus() => cb2.SetGemStatus(level, CrashBandicoot2SaveData.GemType.AllBoxesGem, true);

        _ = Assert.Throws<ArgumentOutOfRangeException>(GetProgressStatus);
        _ = Assert.Throws<ArgumentOutOfRangeException>(SetProgressStatus);
        _ = Assert.Throws<ArgumentOutOfRangeException>(GetCrystalStatus);
        _ = Assert.Throws<ArgumentOutOfRangeException>(SetCrystalStatus);
        _ = Assert.Throws<ArgumentOutOfRangeException>(GetGemStatus);
        _ = Assert.Throws<ArgumentOutOfRangeException>(SetGemStatus);
    }

    [Theory]
    [InlineData(26), InlineData(27)]
    public void CrystalRelatedMethodsThrowIoeWithWrongNumber(int level)
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        void GetCrystalStatus() => cb2.GetCrystalStatus(level);
        void SetCrystalStatus() => cb2.SetCrystalStatus(level, true);

        _ = Assert.Throws<InvalidOperationException>(GetCrystalStatus);
        _ = Assert.Throws<InvalidOperationException>(SetCrystalStatus);
    }

    [Theory]
    [InlineData(4), InlineData(5), InlineData(6), InlineData(8), InlineData(9), InlineData(13)]
    [InlineData(15), InlineData(16), InlineData(22), InlineData(24), InlineData(26), InlineData(27)]
    public void GemRelatedMethodsThrowIoeWithWrongNumber(int level)
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        void GetGemStatus() => cb2.GetGemStatus(level, CrashBandicoot2SaveData.GemType.SecondGem);
        void SetGemStatus() => cb2.SetGemStatus(level, CrashBandicoot2SaveData.GemType.SecondGem, true);

        _ = Assert.Throws<InvalidOperationException>(GetGemStatus);
        _ = Assert.Throws<InvalidOperationException>(SetGemStatus);
    }

    [Theory]
    [InlineData(0), InlineData(3)]
    public void GemRelatedMethodsThrowIeAe(int enumIntValue)
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);
        var enumValue = (CrashBandicoot2SaveData.GemType)enumIntValue;

        void GetGemStatus() => cb2.GetGemStatus(1, enumValue);
        void SetGemStatus() => cb2.SetGemStatus(1, enumValue, true);

        _ = Assert.Throws<InvalidEnumArgumentException>(GetGemStatus);
        _ = Assert.Throws<InvalidEnumArgumentException>(SetGemStatus);
    }

    [Fact]
    public void GetProgressStatusReturnsTrueForLevel1()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        bool traversed = cb2.GetProgressStatus(1);

        Assert.True(traversed);
    }

    [Fact]
    public void GetProgressStatusReturnsFalseForOtherLevels()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        for (int level = 2; level < 28; level++)
        {
            bool traversed = cb2.GetProgressStatus(level);
            Assert.False(traversed);
        }
    }

    [Theory]
    [InlineData(1, false), InlineData(1, true), InlineData(8, false), InlineData(8, true)]
    [InlineData(14, false), InlineData(14, true), InlineData(22, false), InlineData(22, true)]
    public void TestSetProgressStatus(int level, bool traversed)
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        cb2.SetProgressStatus(level, traversed);

        Assert.Equal(traversed, cb2.GetProgressStatus(level));
    }

    [Fact]
    public void GetCrystalStatusReturnsTrueForLevel1()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        bool collected = cb2.GetCrystalStatus(1);

        Assert.True(collected);
    }

    [Fact]
    public void GetCrystalStatusReturnsFalseForOtherLevels()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        for (int level = 2; level < 26; level++)
        {
            bool collected = cb2.GetCrystalStatus(level);
            Assert.False(collected);
        }
    }

    [Theory]
    [InlineData(1, false), InlineData(1, true), InlineData(5, false), InlineData(5, true)]
    [InlineData(9, false), InlineData(9, true), InlineData(18, false), InlineData(18, true)]
    public void TestSetCrystalStatus(int level, bool collected)
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        cb2.SetCrystalStatus(level, collected);

        Assert.Equal(collected, cb2.GetCrystalStatus(level));
    }

    [Fact]
    public void GetAbGemStatusReturnsFalseForAllLevels()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        for (int level = 1; level < 28; level++)
        {
            bool collected = cb2.GetGemStatus(level, CrashBandicoot2SaveData.GemType.AllBoxesGem);
            Assert.False(collected);
        }
    }

    [Fact]
    public void Get2ndGemStatusReturnsFalseForAllLevels()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        int[] levelsWithSecondGem = { 1, 2, 3, 7, 10, 11, 12, 14, 17, 18, 19, 20, 21, 23, 25 };
        for (int i = 0; i < levelsWithSecondGem.Length; i++)
        {
            bool collected = cb2.GetGemStatus(levelsWithSecondGem[i], CrashBandicoot2SaveData.GemType.SecondGem);
            Assert.False(collected);
        }
    }

    [Theory]
    [InlineData(1, CrashBandicoot2SaveData.GemType.AllBoxesGem, false), InlineData(1, CrashBandicoot2SaveData.GemType.AllBoxesGem, true)]
    [InlineData(1, CrashBandicoot2SaveData.GemType.SecondGem, false), InlineData(1, CrashBandicoot2SaveData.GemType.SecondGem, true)]
    [InlineData(10, CrashBandicoot2SaveData.GemType.AllBoxesGem, false), InlineData(10, CrashBandicoot2SaveData.GemType.AllBoxesGem, true)]
    [InlineData(10, CrashBandicoot2SaveData.GemType.SecondGem, false), InlineData(10, CrashBandicoot2SaveData.GemType.SecondGem, true)]
    [InlineData(19, CrashBandicoot2SaveData.GemType.AllBoxesGem, false), InlineData(19, CrashBandicoot2SaveData.GemType.AllBoxesGem, true)]
    [InlineData(19, CrashBandicoot2SaveData.GemType.SecondGem, false), InlineData(19, CrashBandicoot2SaveData.GemType.SecondGem, true)]
    public void TestSetGemStatus(int level, CrashBandicoot2SaveData.GemType gemType, bool collected)
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        cb2.SetGemStatus(level, gemType, collected);

        Assert.Equal(collected, cb2.GetGemStatus(level, gemType));
    }

    [Fact]
    public void FlagOnRemainsOn()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        cb2.SetProgressStatus(1, true);
        cb2.SetCrystalStatus(1, true);

        Assert.True(cb2.GetProgressStatus(1));
        Assert.True(cb2.GetCrystalStatus(1));
    }

    [Fact]
    public void FlagOffRemainsOff()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        var cb2 = new CrashBandicoot2SaveData(fs);

        cb2.SetProgressStatus(2, false);
        cb2.SetCrystalStatus(2, false);
        cb2.SetGemStatus(2, CrashBandicoot2SaveData.GemType.AllBoxesGem, false);
        cb2.SetGemStatus(2, CrashBandicoot2SaveData.GemType.SecondGem, false);

        Assert.False(cb2.GetProgressStatus(2));
        Assert.False(cb2.GetCrystalStatus(2));
        Assert.False(cb2.GetGemStatus(2, CrashBandicoot2SaveData.GemType.AllBoxesGem));
        Assert.False(cb2.GetGemStatus(2, CrashBandicoot2SaveData.GemType.SecondGem));
    }
}
