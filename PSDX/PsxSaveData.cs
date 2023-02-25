using System;
using System.ComponentModel;
using System.IO;

namespace PSDX;

/// <summary>
/// Provides methods for accessing and editing the header of PSX save data files, which is common to all games.
/// </summary>
/// <remarks>Only Single Save Format files (.MCS) are supported.</remarks>
public class PsxSaveData
{
    /// <summary>
    /// 8192 (1 block) + 128 (header sector) bytes.
    /// </summary>
    private const int _saveDataLength = 8320;

    private const int _fileNameOffset = 0xA;

    /// <summary>
    /// 20 bytes, discarding the null-terminator.
    /// </summary>
    private const int _fileNameLength = 20;

    protected MemoryStream Stream { get; } = new(_saveDataLength);

    /// <summary>
    /// Initializes a new instance of the <c>PsxSaveData</c> class with the content of the provided stream.
    /// </summary>
    /// <param name="s">A stream containing any PSX game save data.</param>
    /// <exception cref="ArgumentNullException">The provided stream is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The length of the provided stream does not match the size of a Single Save Format file (.MCS), which is 8320 bytes.
    /// </exception>
    public PsxSaveData(Stream s)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        if (s.Length != _saveDataLength)
        {
            throw new ArgumentException($"The size of Single Save Format files (.MCS) must be {_saveDataLength} bytes.", nameof(s));
        }

        // Copy the stream so that we won't change the original data.
        s.Position = 0;
        s.CopyTo(Stream);
    }

    /// <summary>
    /// Gets the stream containing the changes (if any) made to the save data file.
    /// </summary>
    // Return a copy so that consumers won't interfere.
    public virtual MemoryStream GetStream() => new(Stream.ToArray());

    /// <summary>
    /// Gets the file name of the current game.
    /// </summary>
    public string GetFileName()
    {
        Stream.Position = _fileNameOffset;
        byte[] buffer = new byte[_fileNameLength];
        Stream.ReadExactly(buffer);
        return System.Text.Encoding.ASCII.GetString(buffer);
    }
}

/// <summary>
/// Provides methods for accessing and editing Crash Bandicoot 2 save data files.
/// </summary>
public class CrashBandicoot2SaveData : PsxSaveData
{
    /// <summary>
    /// Only the European version is currently supported.
    /// </summary>
    private const string _serialNumber = "BESCES-00967";

    private const int _maxLevelNumber = 27;

    private const int _maxBossNumber = 5;

    private const int _checksumOffset = 0x1A4;

    private const int _akuAkuOffset = 0x1B4;

    private const int _progressOffset = 0x1BC;

    private const int _crystalsOffset = 0x1C4;

    private const int _gemsOffset = 0x1CC;

    private const int _secretsOffset = 0x1B8;

    /// <summary>
    /// Flags and relative offsets of progress, crystal and all-boxes-gem for each level.
    /// </summary>
    private static readonly (byte Flag, byte Offset)[] _commonInfo = new (byte Flag, byte Offset)[_maxLevelNumber]
    {
        (0x40, 3), (0x40, 1), (0x02, 3), (0x80, 3), (0x01, 3),
        (0x02, 2), (0x01, 4), (0x20, 3), (0x08, 3), (0x08, 4),
        (0x02, 4), (0x04, 1), (0x04, 4), (0x40, 2), (0x80, 2),
        (0x20, 1), (0x20, 2), (0x08, 2), (0x80, 1), (0x10, 4),
        (0x01, 2), (0x04, 2), (0x10, 1), (0x04, 3), (0x40, 4),
        (0x20, 4), (0x80, 4)
    };

    /// <summary>
    /// Flags and relative offsets of the second gem for levels that have it.
    /// </summary>
    private static readonly (byte Flag, byte Offset)?[] _secondGemInfo = new (byte Flag, byte Offset)?[_maxLevelNumber]
    {
        (0x20, 7), (0x04, 7), (0x02, 0), null,      null,
        null,      (0x04, 0), null,      null,      (0x08, 7),
        (0x40, 7), (0x08, 0), null,      (0x10, 0), null,
        null,      (0x01, 1), (0x02, 1), (0x02, 7), (0x10, 7),
        (0x20, 0), null,      (0x40, 0), null,      (0x80, 0),
        null,      null
    };

    /// <summary>
    /// Flags and relative offsets of each boss.
    /// </summary>
    private static readonly (byte Flag, byte Offset)[] _bossInfo = new (byte Flag, byte Offset)[_maxBossNumber]
    {
        (0x40, 0), (0x01, 1), (0x08, 0), (0x02, 1), (0x80, 0)
    };

    /// <summary>
    /// Checks whether the specified <paramref name="level"/> number exists, and throws an exception if not.
    /// </summary>
    /// <param name="level">The number of the level to check for.</param>
    /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="level"/> number is less than one or greater than twenty-seven.</exception>
    private static void CheckLevelNumber(int level)
    {
        if (level < 1 || level > _maxLevelNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "The specified level does not exist.");
        }
    }

    /// <summary>
    /// Checks whether the specified <paramref name="level"/> number contains a crystal, and throws an exception if not.
    /// </summary>
    /// <param name="level">The number of the level to check for.</param>
    /// <exception cref="InvalidOperationException">The specified <paramref name="level"/> number does not contain a crystal.</exception>
    private static void CheckCrystalNumber(int level)
    {
        if (level == 26 || level == 27)
        {
            throw new InvalidOperationException($"Level {level} does not contain a crystal.");
        }
    }

    /// <summary>
    /// Checks whether the specified boss number exists, and throws an exception if not.
    /// </summary>
    /// <param name="bossNumber">The number of the boss to check for.</param>
    /// <exception cref="ArgumentOutOfRangeException">The specified boss number is less than one or greater than five.</exception>
    private static void CheckBossNumber(int bossNumber)
    {
        if (bossNumber < 1 || bossNumber > _maxBossNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(bossNumber), bossNumber, "The specified boss does not exist.");
        }
    }

    /// <summary>
    /// Gets information about the specified gem type for the specified <paramref name="level"/> number.
    /// </summary>
    /// <param name="level">The number of the level to get the gem information of.</param>
    /// <param name="gemType">The type of gem to get the information of.</param>
    /// <returns>The flag and relative offset of the specified gem type for the specified <paramref name="level"/> number.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="level"/> number is less than one or greater than twenty-seven.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="gemType"/> is <see cref="GemType.SecondGem"/> and the specified <paramref name="level"/> does not contain that type of gem.
    /// </exception>
    /// <exception cref="InvalidEnumArgumentException">The specified <paramref name="gemType"/> is not a valid enum value.</exception>
    private static (byte Flag, byte Offset) GetLevelGemInfo(int level, GemType gemType)
    {
        CheckLevelNumber(level);

        if (gemType == GemType.AllBoxesGem)
        {
            return _commonInfo[level - 1];
        }

        if (gemType == GemType.SecondGem)
        {
            var levelGemInfo = _secondGemInfo[level - 1];
            if (levelGemInfo == null)
            {
                throw new InvalidOperationException($"Level {level} does not contain the second gem.");
            }

            return levelGemInfo.Value;
        }

        throw new InvalidEnumArgumentException(nameof(gemType), (int)gemType, typeof(GemType));
    }

    /// <summary>
    /// Gets the value of the flag which determines whether the secret exit has been found in the specified <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The number of the level to get the flag of.</param>
    /// <returns>The value of the flag.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="level"/> number is less than one or greater than twenty-seven.</exception>
    /// <exception cref="InvalidOperationException">The specified <paramref name="level"/> does not contain a secret exit.</exception>
    private static byte GetSecretExitFlag(int level)
    {
        CheckLevelNumber(level);
        return level switch
        {
            7 => 0x10,
            13 => 0x08,
            15 => 0x02,
            16 => 0x04,
            17 => 0x01,
            _ => throw new InvalidOperationException($"Level {level} does not contain a secret exit.")
        };
    }

    /// <summary>
    /// Determines whether the <paramref name="flag"/> located at the specified <paramref name="offset"/> is on or off.
    /// </summary>
    /// <param name="offset">The absolute offset of the flag within the save data file.</param>
    /// <param name="flag">The value of the flag to test.</param>
    /// <returns><see langword="true"/> if the flag is on, otherwise <see langword="false"/>.</returns>
    private bool GetFlag(int offset, int flag)
    {
        Stream.Position = offset;
        int storedFlag = Stream.ReadByte();
        // We can't just return (storedFlag > 0) because different levels can share the same slot.
        return (storedFlag & flag) > 0;
    }

    /// <summary>
    /// Turns the <paramref name="flag"/> located at the specified <paramref name="offset"/> on or off.
    /// </summary>
    /// <param name="offset">The absolute offset of the flag within the save data file.</param>
    /// <param name="flag">The value of the flag to set.</param>
    /// <param name="on">Determines whether to turn the flag on or off.</param>
    private void SetFlag(int offset, int flag, bool on)
    {
        Stream.Position = offset;
        int storedFlag = Stream.ReadByte();
        if (on)
        {
            storedFlag |= flag;
        }
        else
        {
            storedFlag &= ~flag;
        }

        Stream.Position = offset;
        Stream.WriteByte((byte)storedFlag);
    }

    /// <summary>
    /// Represents the two types of gem available in the game.
    /// </summary>
    public enum GemType
    {
        /// <summary>
        /// The gem obtained by destroying all the boxes in a level.<br/>
        /// All levels in the game have this type of gem.
        /// </summary>
        AllBoxesGem = 1,

        /// <summary>
        /// The gem obtained by completing a level-specific task.<br/>
        /// Not all levels in the game have this type of gem.
        /// </summary>
        SecondGem = 2
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="GetStream"/> should compute the right checksum
    /// when called. The default value is <see langword="true"/>. See also <see cref="SetChecksum"/>.
    /// </summary>
    public bool ComputeRightChecksum { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <c>CrashBandicoot2SaveData</c> class with the content of the provided stream.
    /// </summary>
    /// <remarks>Only the European version of Crash Bandicoot 2 is currently supported.</remarks>
    /// <param name="s">A stream containing Crash Bandicoot 2 save data.</param>
    /// <exception cref="ArgumentNullException">The provided stream is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The length of the provided stream does not match the size of a Single Save Format file (.MCS), which is 8320 bytes,
    /// or the stream does not contain Crash Bandicoot 2 save data (European version).
    /// </exception>
    public CrashBandicoot2SaveData(Stream s) : base(s)
    {
        if (!GetFileName().StartsWith(_serialNumber, StringComparison.InvariantCulture))
        {
            throw new ArgumentException("Only the European version of Crash Bandicoot 2 is currently supported.", nameof(s));
        }
    }

    public override MemoryStream GetStream()
    {
        if (ComputeRightChecksum)
        {
            SetChecksum(ComputeChecksum());
        }

        return new MemoryStream(Stream.ToArray());
    }

    /// <summary>
    /// Gets the checksum currently stored in the save data file.<br/>The checksum is used by the game to test for
    /// data integrity, and is not expected to be accessible by players from within the game. It is provided for
    /// the sake of completeness.<br/>Note that the checksum is only updated when the <see cref="GetStream"/>
    /// or <see cref="SetChecksum"/> methods are called. To get the up-to-date value after any change, call
    /// the <see cref="ComputeChecksum"/> method.
    /// </summary>
    public uint GetChecksum()
    {
        byte[] bytes = new byte[sizeof(uint)];
        Stream.Position = _checksumOffset;
        Stream.ReadExactly(bytes);
        return BitConverter.ToUInt32(bytes);
    }

    /// <summary>
    /// Computes the current value of the checksum. The checksum changes after any Set*() method is called, but it
    /// is not stored in the save data file until the <see cref="GetStream"/> or <see cref="SetChecksum"/>
    /// methods are called.
    /// </summary>
    // Thanks to https://github.com/socram8888/tonyhax/blob/9d57fd2e072a4fd173218c321520051479d14012/entrypoints/fix-crash-checksum.sh
    public uint ComputeChecksum()
    {
        byte[] buffer = new byte[0x2A4 * 4];
        Stream.Position = 0x180;
        Stream.ReadExactly(buffer);

        uint checksum = 0x12345678;
        for (int i = 0; i < buffer.Length; i += 4)
        {
            // Skip the location where the checksum itself is stored.
            // Tenth word in the buffer, so index is 4 * (10 - 1).
            if (i == 36)
            {
                i += 4;
            }

            byte[] wordBytes = buffer[i..(i + 4)];
            uint word = BitConverter.ToUInt32(wordBytes, 0);
            checksum = (checksum + word) & 0xFFFFFFFF;
        }

        return checksum;
    }

    /// <summary>
    /// Sets the checksum to store in the save data file. An incorrect value will invalidate the save data,
    /// i.e., the game will not load it.<br/>There is no need to call this method: the right checksum is by default
    /// computed and applied to the save data when the <see cref="GetStream"/> method is called, unless the
    /// <see cref="ComputeRightChecksum"/> property has been set to <see langword="false"/>.<br/>This method is provided
    /// to allow for experiments. To take effect, the <see cref="ComputeRightChecksum"/> property must be set to
    /// <see langword="false"/> before calling <see cref="GetStream"/>, otherwise the provided
    /// <paramref name="checksum"/> will be overwritten by the correct value.
    /// </summary>
    /// <param name="checksum">The checksum to store in the save data file.</param>
    public void SetChecksum(uint checksum)
    {
        Stream.Position = _checksumOffset;
        byte[] bytes = BitConverter.GetBytes(checksum);
        Stream.Write(bytes);
    }

    /// <summary>
    /// Gets the number of Aku Aku masks currently stored in the save data file.
    /// </summary>
    public int GetAkuAkuMasks()
    {
        byte[] bytes = new byte[sizeof(int)];
        Stream.Position = _akuAkuOffset;
        Stream.ReadExactly(bytes);
        return BitConverter.ToInt32(bytes);
    }

    /// <summary>
    /// Sets the number of Aku Aku masks to store in the save data file.
    /// </summary>
    /// <param name="number">The number of Aku Aku masks to store.</param>
    public void SetAkuAkuMasks(int number)
    {
        Stream.Position = _akuAkuOffset;
        byte[] bytes = BitConverter.GetBytes(number);
        Stream.Write(bytes);
    }

    /// <summary>
    /// Gets a value indicating whether the specified <paramref name="level"/> has been traversed, i.e. its exit has been reached.
    /// </summary>
    /// <param name="level">The number of the level to check for the progress status.</param>
    /// <returns><see langword="true"/> if the level has been traversed, otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="level"/> number is less than one or greater than twenty-seven.</exception>
    public bool GetProgressStatus(int level)
    {
        CheckLevelNumber(level);

        (byte levelProgressFlag, byte levelProgressOffset) = _commonInfo[level - 1];
        int progressOffset = _progressOffset + levelProgressOffset;
        return GetFlag(progressOffset, levelProgressFlag);
    }

    /// <summary>
    /// Sets a value indicating whether the specified <paramref name="level"/> has been traversed, i.e. its exit has been reached.
    /// </summary>
    /// <param name="level">The number of the level to set the progress status of.</param>
    /// <param name="traversed">Determines whether the level has been traversed.</param>
    /// <exception cref="ArgumentOutOfRangeException">The specified level number is less than one or greater than twenty-seven.</exception>
    public void SetProgressStatus(int level, bool traversed)
    {
        CheckLevelNumber(level);

        (byte levelProgressFlag, byte levelProgressOffset) = _commonInfo[level - 1];
        int progressOffset = _progressOffset + levelProgressOffset;
        SetFlag(progressOffset, levelProgressFlag, traversed);
    }

    /// <summary>
    /// Gets a value indicating whether the crystal has been collected in the specified <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The number of the level to check for the crystal status.</param>
    /// <returns><see langword="true"/> if the crystal has been collected, otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="level"/> number is less than one or greater than twenty-seven.</exception>
    /// <exception cref="InvalidOperationException">The specified <paramref name="level"/> does not contain a crystal.</exception>
    public bool GetCrystalStatus(int level)
    {
        CheckLevelNumber(level);
        CheckCrystalNumber(level);

        (byte levelCrystalFlag, byte levelCrystalOffset) = _commonInfo[level - 1];
        int crystalOffset = _crystalsOffset + levelCrystalOffset;
        return GetFlag(crystalOffset, levelCrystalFlag);
    }

    /// <summary>
    /// Sets a value indicating whether the crystal has been collected in the specified <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The number of the level to set the crystal status of.</param>
    /// <param name="collected">Determines whether the crystal has been collected in the level.</param>
    /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="level"/> number is less than one or greater than twenty-seven.</exception>
    /// <exception cref="InvalidOperationException">The specified <paramref name="level"/> does not contain a crystal.</exception>
    public void SetCrystalStatus(int level, bool collected)
    {
        CheckLevelNumber(level);
        CheckCrystalNumber(level);

        (byte levelCrystalFlag, byte levelCrystalOffset) = _commonInfo[level - 1];
        int crystalOffset = _crystalsOffset + levelCrystalOffset;
        SetFlag(crystalOffset, levelCrystalFlag, collected);
    }

    /// <summary>
    /// Gets a value indicating whether the specified gem type has been collected in the specified <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The number of the level to check for the gem status.</param>
    /// <param name="gemType">The type of gem to check the status of.</param>
    /// <returns><see langword="true"/> if the gem has been collected, otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="level"/> number is less than one or greater than twenty-seven.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="gemType"/> is <see cref="GemType.SecondGem"/> and the specified <paramref name="level"/> does not contain that type of gem.
    /// </exception>
    /// <exception cref="InvalidEnumArgumentException">The specified <paramref name="gemType"/> is not a valid enum value.</exception>
    public bool GetGemStatus(int level, GemType gemType)
    {
        CheckLevelNumber(level);

        (byte levelGemFlag, byte levelGemOffset) = GetLevelGemInfo(level, gemType);

        int gemOffset = _gemsOffset + levelGemOffset;
        return GetFlag(gemOffset, levelGemFlag);
    }

    /// <summary>
    /// Sets a value indicating whether the specified gem type has been collected in the specified <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The number of the level to set the gem status of.</param>
    /// <param name="gemType">The type of gem to set the status of.</param>
    /// <param name="collected">Determines whether the gem has been collected in the level.</param>
    /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="level"/> number is less than one or greater than twenty-seven.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="gemType"/> is <see cref="GemType.SecondGem"/> and the specified <paramref name="level"/> does not contain that type of gem.
    /// </exception>
    /// <exception cref="InvalidEnumArgumentException">The specified <paramref name="gemType"/> is not a valid enum value.</exception>
    public void SetGemStatus(int level, GemType gemType, bool collected)
    {
        CheckLevelNumber(level);

        (byte levelGemFlag, byte levelGemOffset) = GetLevelGemInfo(level, gemType);

        int gemOffset = _gemsOffset + levelGemOffset;
        SetFlag(gemOffset, levelGemFlag, collected);
    }

    /// <summary>
    /// Gets a value indicating whether the specified boss has been defeated.
    /// </summary>
    /// <param name="bossNumber">The number of the boss to get the status of.</param>
    /// <returns><see langword="true"/> if the boss has been defeated, otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The specified boss number is less than one or greater than five.</exception>
    public bool GetBossStatus(int bossNumber)
    {
        CheckBossNumber(bossNumber);

        (byte bossFlag, byte bossRelativeOffset) = _bossInfo[bossNumber - 1];
        int bossOffset = _progressOffset + bossRelativeOffset;
        return GetFlag(bossOffset, bossFlag);
    }

    /// <summary>
    /// Sets a value indicating whether the specified boss has been defeated.
    /// </summary>
    /// <param name="bossNumber">The number of the boss to set the status of.</param>
    /// <param name="defeated">Determines whether the boss has been defeated.</param>
    /// <exception cref="ArgumentOutOfRangeException">The specified boss number is less than one or greater than five.</exception>
    public void SetBossStatus(int bossNumber, bool defeated)
    {
        CheckBossNumber(bossNumber);

        (byte bossFlag, byte bossRelativeOffset) = _bossInfo[bossNumber - 1];
        int bossOffset = _progressOffset + bossRelativeOffset;
        SetFlag(bossOffset, bossFlag, defeated);
    }

    /// <summary>
    /// Gets a value indicating whether the secret exit has been found in the specified <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The number of the level to check for.</param>
    /// <returns><see langword="true"/> if the secret exit has been found, otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="level"/> number is less than one or greater than twenty-seven.</exception>
    /// <exception cref="InvalidOperationException">The specified <paramref name="level"/> does not contain a secret exit.</exception>
    public bool GetSecretExitStatus(int level) => GetFlag(_secretsOffset, GetSecretExitFlag(level));

    /// <summary>
    /// Sets a value indicating whether the secret exit has been found in the specified <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The number of the level to set the secret exit status of.</param>
    /// <param name="found">Determines whether the secret exit has been found.</param>
    /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="level"/> number is less than one or greater than twenty-seven.</exception>
    /// <exception cref="InvalidOperationException">The specified <paramref name="level"/> does not contain a secret exit.</exception>
    public void SetSecretExitStatus(int level, bool found) => SetFlag(_secretsOffset, GetSecretExitFlag(level), found);
}
