using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Xunit;

namespace PSDX.Tests;

public class PsdxTests
{
    private static CrashBandicoot2SaveData GetCb2Instance()
    {
        using var fs = new FileStream("cb2.mcs", FileMode.Open);
        return new CrashBandicoot2SaveData(fs);
    }

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
        var cb2 = GetCb2Instance();

        int masks = cb2.GetAkuAkuMasks();

        Assert.Equal(2, masks);
    }

    [Theory]
    [InlineData(-1), InlineData(0), InlineData(1), InlineData(2), InlineData(3), InlineData(4)]
    public void TestSetAkuAkuMasks(int masksToSet)
    {
        var cb2 = GetCb2Instance();

        cb2.SetAkuAkuMasks(masksToSet);

        Assert.Equal(masksToSet, cb2.GetAkuAkuMasks());
    }

    [Fact]
    public void TestGetChecksum()
    {
        var cb2 = GetCb2Instance();

        uint checksum = cb2.GetChecksum();

        Assert.Equal(0xE2BC35B9, checksum);
    }

    [Theory]
    [InlineData(0), InlineData(7), InlineData(0xA14DC582), InlineData(uint.MaxValue)]
    public void TestSetChecksum(uint checksumToSet)
    {
        var cb2 = GetCb2Instance();

        cb2.SetChecksum(checksumToSet);

        Assert.Equal(checksumToSet, cb2.GetChecksum());
    }

    [Fact]
    public void TestComputeChecksum()
    {
        var cb2 = GetCb2Instance();

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
        var cb2 = GetCb2Instance();
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
        var cb2 = GetCb2Instance();

        void GetProgressStatus() => cb2.GetProgressStatus(level);
        void SetProgressStatus() => cb2.SetProgressStatus(level, true);
        void GetCrystalStatus() => cb2.GetCrystalStatus(level);
        void SetCrystalStatus() => cb2.SetCrystalStatus(level, true);
        void GetGemStatus() => cb2.GetGemStatus(level, CrashBandicoot2SaveData.GemType.AllBoxesGem);
        void SetGemStatus() => cb2.SetGemStatus(level, CrashBandicoot2SaveData.GemType.AllBoxesGem, true);
        void GetSecretExitStatus() => cb2.GetSecretExitStatus(level);
        void SetSecretExitStatus() => cb2.SetSecretExitStatus(level, true);

        _ = Assert.Throws<ArgumentOutOfRangeException>(GetProgressStatus);
        _ = Assert.Throws<ArgumentOutOfRangeException>(SetProgressStatus);
        _ = Assert.Throws<ArgumentOutOfRangeException>(GetCrystalStatus);
        _ = Assert.Throws<ArgumentOutOfRangeException>(SetCrystalStatus);
        _ = Assert.Throws<ArgumentOutOfRangeException>(GetGemStatus);
        _ = Assert.Throws<ArgumentOutOfRangeException>(SetGemStatus);
        _ = Assert.Throws<ArgumentOutOfRangeException>(GetSecretExitStatus);
        _ = Assert.Throws<ArgumentOutOfRangeException>(SetSecretExitStatus);
    }

    [Theory]
    [InlineData(26), InlineData(27)]
    public void CrystalRelatedMethodsThrowIoeWithWrongNumber(int level)
    {
        var cb2 = GetCb2Instance();

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
        var cb2 = GetCb2Instance();

        void GetGemStatus() => cb2.GetGemStatus(level, CrashBandicoot2SaveData.GemType.SecondGem);
        void SetGemStatus() => cb2.SetGemStatus(level, CrashBandicoot2SaveData.GemType.SecondGem, true);

        _ = Assert.Throws<InvalidOperationException>(GetGemStatus);
        _ = Assert.Throws<InvalidOperationException>(SetGemStatus);
    }

    [Theory]
    [InlineData(0), InlineData(3)]
    public void GemRelatedMethodsThrowIeAe(int enumIntValue)
    {
        var cb2 = GetCb2Instance();
        var enumValue = (CrashBandicoot2SaveData.GemType)enumIntValue;

        void GetGemStatus() => cb2.GetGemStatus(1, enumValue);
        void SetGemStatus() => cb2.SetGemStatus(1, enumValue, true);

        _ = Assert.Throws<InvalidEnumArgumentException>(GetGemStatus);
        _ = Assert.Throws<InvalidEnumArgumentException>(SetGemStatus);
    }

    [Fact]
    public void GetProgressStatusReturnsTrueForLevel1()
    {
        var cb2 = GetCb2Instance();

        bool traversed = cb2.GetProgressStatus(1);

        Assert.True(traversed);
    }

    [Fact]
    public void GetProgressStatusReturnsFalseForOtherLevels()
    {
        var cb2 = GetCb2Instance();

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
        var cb2 = GetCb2Instance();

        cb2.SetProgressStatus(level, traversed);

        Assert.Equal(traversed, cb2.GetProgressStatus(level));
    }

    [Fact]
    public void GetCrystalStatusReturnsTrueForLevel1()
    {
        var cb2 = GetCb2Instance();

        bool collected = cb2.GetCrystalStatus(1);

        Assert.True(collected);
    }

    [Fact]
    public void GetCrystalStatusReturnsFalseForOtherLevels()
    {
        var cb2 = GetCb2Instance();

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
        var cb2 = GetCb2Instance();

        cb2.SetCrystalStatus(level, collected);

        Assert.Equal(collected, cb2.GetCrystalStatus(level));
    }

    [Fact]
    public void GetAbGemStatusReturnsFalseForAllLevels()
    {
        var cb2 = GetCb2Instance();

        for (int level = 1; level < 28; level++)
        {
            bool collected = cb2.GetGemStatus(level, CrashBandicoot2SaveData.GemType.AllBoxesGem);
            Assert.False(collected);
        }
    }

    [Fact]
    public void Get2ndGemStatusReturnsFalseForAllLevels()
    {
        var cb2 = GetCb2Instance();

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
        var cb2 = GetCb2Instance();

        cb2.SetGemStatus(level, gemType, collected);

        Assert.Equal(collected, cb2.GetGemStatus(level, gemType));
    }

    [Fact]
    public void FlagOnRemainsOn()
    {
        var cb2 = GetCb2Instance();

        cb2.SetProgressStatus(1, true);
        cb2.SetCrystalStatus(1, true);

        Assert.True(cb2.GetProgressStatus(1));
        Assert.True(cb2.GetCrystalStatus(1));
    }

    [Fact]
    public void FlagOffRemainsOff()
    {
        var cb2 = GetCb2Instance();

        cb2.SetProgressStatus(2, false);
        cb2.SetCrystalStatus(2, false);
        cb2.SetGemStatus(2, CrashBandicoot2SaveData.GemType.AllBoxesGem, false);
        cb2.SetGemStatus(2, CrashBandicoot2SaveData.GemType.SecondGem, false);
        cb2.SetBossStatus(2, false);

        Assert.False(cb2.GetProgressStatus(2));
        Assert.False(cb2.GetCrystalStatus(2));
        Assert.False(cb2.GetGemStatus(2, CrashBandicoot2SaveData.GemType.AllBoxesGem));
        Assert.False(cb2.GetGemStatus(2, CrashBandicoot2SaveData.GemType.SecondGem));
        Assert.False(cb2.GetBossStatus(2));
    }

    [Theory]
    [InlineData(-1), InlineData(0), InlineData(6)]
    public void BossRelatedMethodsThrowAoReWithWrongNumber(int bossNumber)
    {
        var cb2 = GetCb2Instance();

        void GetBossStatus() => cb2.GetBossStatus(bossNumber);
        void SetBossStatus() => cb2.SetBossStatus(bossNumber, true);

        _ = Assert.Throws<ArgumentOutOfRangeException>(GetBossStatus);
        _ = Assert.Throws<ArgumentOutOfRangeException>(SetBossStatus);
    }

    [Fact]
    public void GetBossStatusReturnsFalseForAllBosses()
    {
        var cb2 = GetCb2Instance();

        for (int bossNumber = 1; bossNumber < 6; bossNumber++)
        {
            bool defeated = cb2.GetBossStatus(bossNumber);
            Assert.False(defeated);
        }
    }

    [Theory]
    [InlineData(1, false), InlineData(1, true), InlineData(2, false), InlineData(2, true)]
    [InlineData(3, false), InlineData(3, true), InlineData(4, false), InlineData(4, true)]
    [InlineData(5, false), InlineData(5, true)]
    public void TestSetBossStatus(int bossNumber, bool defeated)
    {
        var cb2 = GetCb2Instance();

        cb2.SetBossStatus(bossNumber, defeated);

        Assert.Equal(defeated, cb2.GetBossStatus(bossNumber));
    }

    [Theory]
    [InlineData(1), InlineData(2), InlineData(3), InlineData(4), InlineData(5), InlineData(6)]
    [InlineData(8), InlineData(9), InlineData(10), InlineData(11), InlineData(12), InlineData(14)]
    [InlineData(18), InlineData(19), InlineData(20), InlineData(21), InlineData(22), InlineData(23)]
    [InlineData(24), InlineData(25), InlineData(26), InlineData(27)]
    public void SecretExitRelatedMethodsThrowIoeWithWrongNumber(int level)
    {
        var cb2 = GetCb2Instance();

        void GetSecretExitStatus() => cb2.GetSecretExitStatus(level);
        void SetSecretExitStatus() => cb2.SetSecretExitStatus(level, true);

        _ = Assert.Throws<InvalidOperationException>(GetSecretExitStatus);
        _ = Assert.Throws<InvalidOperationException>(SetSecretExitStatus);
    }

    [Fact]
    public void GetSecretExitStatusReturnsFalseForAllLevels()
    {
        var cb2 = GetCb2Instance();

        int[] levelsWithSecretExit = { 7, 13, 15, 16, 17 };
        for (int i = 0; i < levelsWithSecretExit.Length; i++)
        {
            bool found = cb2.GetSecretExitStatus(levelsWithSecretExit[i]);
            Assert.False(found);
        }
    }

    [Theory]
    [InlineData(7, true), InlineData(13, true), InlineData(15, true), InlineData(16, true), InlineData(17, true)]
    [InlineData(7, false), InlineData(13, false), InlineData(15, false), InlineData(16, false), InlineData(17, false)]
    public void TestSetSecretExitStatus(int level, bool found)
    {
        var cb2 = GetCb2Instance();

        cb2.SetSecretExitStatus(level, found);

        Assert.Equal(found, cb2.GetSecretExitStatus(level));
    }

    [Fact]
    public void GetPolarTrickStatusReturnsFalse()
    {
        var cb2 = GetCb2Instance();

        bool performed = cb2.GetPolarTrickStatus();

        Assert.False(performed);
    }

    [Theory]
    [InlineData(true), InlineData(false)]
    public void TestSetPolarTrickStatus(bool performed)
    {
        var cb2 = GetCb2Instance();

        cb2.SetPolarTrickStatus(performed);

        Assert.Equal(performed, cb2.GetPolarTrickStatus());
    }

    [Fact]
    public void GetLastPlayedLevelReturns1()
    {
        var cb2 = GetCb2Instance();

        int level = cb2.GetLastPlayedLevel();

        Assert.Equal(1, level);
    }

    [Theory]
    [InlineData(-1), InlineData(0), InlineData(33)]
    public void SetLastPlayedLevelThrowsAoReWithWrongNumber(int level)
    {
        var cb2 = GetCb2Instance();

        void SetLastPlayedLevel() => cb2.SetLastPlayedLevel(level);

        _ = Assert.Throws<ArgumentOutOfRangeException>(SetLastPlayedLevel);
    }

    [Fact]
    public void TestSetLastPlayedLevel()
    {
        var cb2 = GetCb2Instance();

        for (int level = 1; level < 33; level++)
        {
            cb2.SetLastPlayedLevel(level);
            Assert.Equal(level, cb2.GetLastPlayedLevel());
        }
    }

    [Fact]
    public void GetUsernameReturnsCrashB()
    {
        var cb2 = GetCb2Instance();

        string username = cb2.GetUsername();

        Assert.Equal("CRASH B", username);
    }

    [Fact]
    public void SetUsernameThrowsAne()
    {
        var cb2 = GetCb2Instance();

        void CodeToTest() => cb2.SetUsername(null!); // Intentionally suppress the (right) warning.

        _ = Assert.Throws<ArgumentNullException>(CodeToTest);
    }

    [Fact]
    public void SetUsernameThrowsAe()
    {
        var cb2 = GetCb2Instance();

        void CodeToTest() => cb2.SetUsername("123456789");

        _ = Assert.Throws<ArgumentException>(CodeToTest);
    }

    [Theory]
    [InlineData("ABCDEFGH"), InlineData("IJKLMNOP"), InlineData("QRSTUVWX"), InlineData("YZ")]
    [InlineData("A BC DE"), InlineData("")]
    public void TestSetUsername(string name)
    {
        var cb2 = GetCb2Instance();

        cb2.SetUsername(name);

        Assert.Equal(name, cb2.GetUsername());
    }

    [Fact]
    public void GetLanguageReturnsIt()
    {
        var cb2 = GetCb2Instance();

        var language = cb2.GetLanguage();

        Assert.Equal(CrashBandicoot2SaveData.Language.Italian, language);
    }

    [Theory]
    [InlineData(CrashBandicoot2SaveData.Language.English), InlineData(CrashBandicoot2SaveData.Language.Spanish)]
    [InlineData(CrashBandicoot2SaveData.Language.French), InlineData(CrashBandicoot2SaveData.Language.German)]
    [InlineData(CrashBandicoot2SaveData.Language.Italian), InlineData((CrashBandicoot2SaveData.Language)5)]
    public void TestSetLanguage(CrashBandicoot2SaveData.Language language)
    {
        var cb2 = GetCb2Instance();

        cb2.SetLanguage(language);

        Assert.Equal(language, cb2.GetLanguage());
    }

    [Fact]
    public void GetAudioTypeReturnsStereo()
    {
        var cb2 = GetCb2Instance();

        var audioType = cb2.GetAudioType();

        Assert.Equal(CrashBandicoot2SaveData.AudioType.Stereo, audioType);
    }

    [Theory]
    [InlineData(CrashBandicoot2SaveData.AudioType.Stereo), InlineData(CrashBandicoot2SaveData.AudioType.Mono)]
    [InlineData((CrashBandicoot2SaveData.AudioType)2)]
    public void TestSetAudioType(CrashBandicoot2SaveData.AudioType audioType)
    {
        var cb2 = GetCb2Instance();

        cb2.SetAudioType(audioType);

        Assert.Equal(audioType, cb2.GetAudioType());
    }

    [Fact]
    public void GetLivesReturns4()
    {
        var cb2 = GetCb2Instance();

        int lives = cb2.GetLives();

        Assert.Equal(4, lives);
    }

    [Theory]
    [InlineData(-1), InlineData(0), InlineData(1), InlineData(100), InlineData(int.MaxValue)]
    public void TestSetLives(int lives)
    {
        var cb2 = GetCb2Instance();

        cb2.SetLives(lives);

        Assert.Equal(lives, cb2.GetLives());
    }

    [Fact]
    public void GetWumpaFruitsReturns11()
    {
        var cb2 = GetCb2Instance();

        int fruits = cb2.GetWumpaFruits();

        Assert.Equal(11, fruits);
    }

    [Theory]
    [InlineData(-1), InlineData(0), InlineData(6), InlineData(109), InlineData(int.MaxValue)]
    public void TestSetWumpaFruits(int fruits)
    {
        var cb2 = GetCb2Instance();

        cb2.SetWumpaFruits(fruits);

        Assert.Equal(fruits, cb2.GetWumpaFruits());
    }

    [Fact]
    public void GetScreenOffsetReturns0()
    {
        var cb2 = GetCb2Instance();

        int offset = cb2.GetScreenOffset();

        Assert.Equal(0, offset);
    }

    [Theory]
    [InlineData(-16), InlineData(-15), InlineData(-14), InlineData(-1), InlineData(0), InlineData(1)]
    [InlineData(4), InlineData(13), InlineData(15), InlineData(18)]
    public void TestSetScreenOffset(int offset)
    {
        var cb2 = GetCb2Instance();

        cb2.SetScreenOffset(offset);

        Assert.Equal(offset, cb2.GetScreenOffset());
    }

    [Fact]
    public void GetEffectsVolumeReturns100()
    {
        var cb2 = GetCb2Instance();

        int volume = cb2.GetEffectsVolume();

        Assert.Equal(100, volume);
    }

    [Fact]
    public void SetEffectsVolumeThrowsAeWithWrongVolume()
    {
        var cb2 = GetCb2Instance();
        int[] unsupportedVolumes =
        {
            int.MinValue, -105, -1, 2, 5, 8, 11, 13, 16, 19, 22, 24, 27, 30, 33, 35, 38, 41, 44, 46, 49, 52,
            55, 57, 60, 63, 66, 68, 71, 74, 77, 79, 82, 85, 88, 91, 93, 96, 99, 101, 121, 215, int.MaxValue
        };

        foreach (int volume in unsupportedVolumes)
        {
            void CodeToTest() => cb2.SetEffectsVolume(volume);

            _ = Assert.Throws<ArgumentException>(CodeToTest);
        }
    }

    [Fact]
    public void TestSetEffectsVolume()
    {
        var cb2 = GetCb2Instance();
        int[] volumeLevels = new int[65]
        {
            0, 1, 3, 4, 6, 7, 9, 10, 12, 14, 15, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 32, 34,
            36, 37, 39, 40, 42, 43, 45, 47, 48, 50, 51, 53, 54, 56, 58, 59, 61, 62, 64, 65, 67,
            69, 70, 72, 73, 75, 76, 78, 80, 81, 83, 84, 86, 87, 89, 90, 92, 94, 95, 97, 98, 100
        };

        foreach (int volume in volumeLevels)
        {
            cb2.SetEffectsVolume(volume);

            Assert.Equal(volume, cb2.GetEffectsVolume());
        }
    }

    [Fact]
    public void GetMusicVolumeReturns100()
    {
        var cb2 = GetCb2Instance();

        int volume = cb2.GetMusicVolume();

        Assert.Equal(100, volume);
    }

    [Fact]
    public void SetMusicVolumeThrowsAeWithWrongVolume()
    {
        var cb2 = GetCb2Instance();
        int[] unsupportedVolumes =
        {
            int.MinValue, -105, -1, 2, 5, 8, 11, 13, 16, 19, 22, 24, 27, 30, 33, 35, 38, 41, 44, 46, 49, 52,
            55, 57, 60, 63, 66, 68, 71, 74, 77, 79, 82, 85, 88, 91, 93, 96, 99, 101, 121, 215, int.MaxValue
        };

        foreach (int volume in unsupportedVolumes)
        {
            void CodeToTest() => cb2.SetMusicVolume(volume);

            _ = Assert.Throws<ArgumentException>(CodeToTest);
        }
    }

    [Fact]
    public void TestSetMusicVolume()
    {
        var cb2 = GetCb2Instance();
        int[] volumeLevels = new int[65]
        {
            0, 1, 3, 4, 6, 7, 9, 10, 12, 14, 15, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 32, 34,
            36, 37, 39, 40, 42, 43, 45, 47, 48, 50, 51, 53, 54, 56, 58, 59, 61, 62, 64, 65, 67,
            69, 70, 72, 73, 75, 76, 78, 80, 81, 83, 84, 86, 87, 89, 90, 92, 94, 95, 97, 98, 100
        };

        foreach (int volume in volumeLevels)
        {
            cb2.SetMusicVolume(volume);

            Assert.Equal(volume, cb2.GetMusicVolume());
        }
    }

    [Fact]
    public void TestGetSlotStatus()
    {
        var cb2 = GetCb2Instance();

        bool slotIsEmpty = cb2.GetSlotStatus(1);
        Assert.False(slotIsEmpty);

        for (int i = 2; i <= 4; i++)
        {
            slotIsEmpty = cb2.GetSlotStatus(i);
            Assert.True(slotIsEmpty);
        }
    }

    [Theory]
    [InlineData(1, false), InlineData(1, true), InlineData(2, false), InlineData(2, true)]
    [InlineData(3, false), InlineData(3, true), InlineData(4, false), InlineData(4, true)]
    public void TestSetSlotStatus(int slotNumber, bool empty)
    {
        var cb2 = GetCb2Instance();

        cb2.SetSlotStatus(slotNumber, empty);

        Assert.Equal(empty, cb2.GetSlotStatus(slotNumber));
    }

    [Theory]
    [InlineData(-1), InlineData(0), InlineData(5)]
    public void MethodsThrowAoReWithWrongSlot(int slotNumber)
    {
        var cb2 = GetCb2Instance();

        var actions = new Action[]
        {
            () => cb2.GetAkuAkuMasks(slotNumber),
            () => cb2.SetAkuAkuMasks(3, slotNumber),
            () => cb2.GetProgressStatus(1, slotNumber),
            () => cb2.SetProgressStatus(1, true, slotNumber),
            () => cb2.GetCrystalStatus(2, slotNumber),
            () => cb2.SetCrystalStatus(2, false, slotNumber),
            () => cb2.GetGemStatus(3, CrashBandicoot2SaveData.GemType.AllBoxesGem, slotNumber),
            () => cb2.SetGemStatus(3, CrashBandicoot2SaveData.GemType.AllBoxesGem, true, slotNumber),
            () => cb2.GetBossStatus(1, slotNumber),
            () => cb2.SetBossStatus(1, true, slotNumber),
            () => cb2.GetSecretExitStatus(7, slotNumber),
            () => cb2.SetSecretExitStatus(13, true, slotNumber),
            () => cb2.GetPolarTrickStatus(slotNumber),
            () => cb2.SetPolarTrickStatus(false, slotNumber),
            () => cb2.GetLastPlayedLevel(slotNumber),
            () => cb2.SetLastPlayedLevel(5, slotNumber),
            () => cb2.GetUsername(slotNumber),
            () => cb2.SetUsername("A", slotNumber),
            () => cb2.GetLanguage(slotNumber),
            () => cb2.SetLanguage(CrashBandicoot2SaveData.Language.English, slotNumber),
            () => cb2.GetAudioType(slotNumber),
            () => cb2.SetAudioType(CrashBandicoot2SaveData.AudioType.Stereo, slotNumber),
            () => cb2.GetLives(slotNumber),
            () => cb2.SetLives(100, slotNumber),
            () => cb2.GetWumpaFruits(slotNumber),
            () => cb2.SetWumpaFruits(50, slotNumber),
            () => cb2.GetScreenOffset(slotNumber),
            () => cb2.SetScreenOffset(0, slotNumber),
            () => cb2.GetEffectsVolume(slotNumber),
            () => cb2.SetEffectsVolume(100, slotNumber),
            () => cb2.GetMusicVolume(slotNumber),
            () => cb2.SetMusicVolume(0, slotNumber),
            () => cb2.GetSlotStatus(slotNumber),
            () => cb2.SetSlotStatus(slotNumber, true)
        };

        foreach (var action in actions)
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(action);
        }
    }
}
