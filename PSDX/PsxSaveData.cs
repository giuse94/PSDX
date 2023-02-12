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

    private const int _checksumOffset = 0x1A4;

    private const int _akuAkuOffset = 0x1B4;

    private const int _progressOffset = 0x1BC;

    private const int _crystalsOffset = 0x1C4;

    private const int _gemsOffset = 0x1CC;

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
        if (level < 1 || level > _maxLevelNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "The specified level does not exist.");
        }

        (byte levelProgressFlag, byte levelProgressOffset) = _commonInfo[level - 1];
        Stream.Position = _progressOffset + levelProgressOffset;
        int progressFlag = Stream.ReadByte();
        // We can't just return (progressFlag > 0) because different levels can share the same slot.
        return (progressFlag & levelProgressFlag) > 0;
    }

    /// <summary>
    /// Sets a value indicating whether the specified <paramref name="level"/> has been traversed, i.e. its exit has been reached.
    /// </summary>
    /// <param name="level">The number of the level to set the progress status of.</param>
    /// <param name="traversed">Determines whether the level has been traversed.</param>
    /// <exception cref="ArgumentOutOfRangeException">The specified level number is less than one or greater than twenty-seven.</exception>
    public void SetProgressStatus(int level, bool traversed)
    {
        if (level < 1 || level > _maxLevelNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "The specified level does not exist.");
        }

        (byte levelProgressFlag, byte levelProgressOffset) = _commonInfo[level - 1];
        int progressOffset = _progressOffset + levelProgressOffset;
        Stream.Position = progressOffset;
        int progressFlag = Stream.ReadByte();
        if (traversed)
        {
            progressFlag |= levelProgressFlag;
        }
        else
        {
            progressFlag &= ~levelProgressFlag;
        }

        Stream.Position = progressOffset;
        Stream.WriteByte((byte)progressFlag);
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
        if (level < 1 || level > _maxLevelNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "The specified level does not exist.");
        }
        if (level == 26 || level == 27)
        {
            throw new InvalidOperationException($"Level {level} does not contain a crystal.");
        }

        (byte levelCrystalFlag, byte levelCrystalOffset) = _commonInfo[level - 1];
        Stream.Position = _crystalsOffset + levelCrystalOffset;
        int crystalFlag = Stream.ReadByte();
        return (crystalFlag & levelCrystalFlag) > 0;
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
        if (level < 1 || level > _maxLevelNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "The specified level does not exist.");
        }
        if (level == 26 || level == 27)
        {
            throw new InvalidOperationException($"Level {level} does not contain a crystal.");
        }

        (byte levelCrystalFlag, byte levelCrystalOffset) = _commonInfo[level - 1];
        int crystalOffset = _crystalsOffset + levelCrystalOffset;
        Stream.Position = crystalOffset;
        int crystalFlag = Stream.ReadByte();
        if (collected)
        {
            crystalFlag |= levelCrystalFlag;
        }
        else
        {
            crystalFlag &= ~levelCrystalFlag;
        }

        Stream.Position = crystalOffset;
        Stream.WriteByte((byte)crystalFlag);
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
        if (level < 1 || level > _maxLevelNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "The specified level does not exist.");
        }

        (byte Flag, byte Offset) levelGemInfo;
        if (gemType == GemType.AllBoxesGem)
        {
            levelGemInfo = _commonInfo[level - 1];
        }
        else if (gemType == GemType.SecondGem)
        {
            levelGemInfo = _secondGemInfo[level - 1].GetValueOrDefault();
        }
        else
        {
            throw new InvalidEnumArgumentException(nameof(gemType), (int)gemType, typeof(GemType));
        }

        if (levelGemInfo == default)
        {
            throw new InvalidOperationException($"Level {level} does not contain the second gem.");
        }

        Stream.Position = _gemsOffset + levelGemInfo.Offset;
        int gemFlag = Stream.ReadByte();
        return (gemFlag & levelGemInfo.Flag) > 0;
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
        if (level < 1 || level > _maxLevelNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "The specified level does not exist.");
        }

        (byte Flag, byte Offset) levelGemInfo;
        if (gemType == GemType.AllBoxesGem)
        {
            levelGemInfo = _commonInfo[level - 1];
        }
        else if (gemType == GemType.SecondGem)
        {
            levelGemInfo = _secondGemInfo[level - 1].GetValueOrDefault();
        }
        else
        {
            throw new InvalidEnumArgumentException(nameof(gemType), (int)gemType, typeof(GemType));
        }

        if (levelGemInfo == default)
        {
            throw new InvalidOperationException($"Level {level} does not contain the second gem.");
        }

        int gemOffset = _gemsOffset + levelGemInfo.Offset;
        Stream.Position = gemOffset;
        int gemFlag = Stream.ReadByte();
        if (collected)
        {
            gemFlag |= levelGemInfo.Flag;
        }
        else
        {
            gemFlag &= ~levelGemInfo.Flag;
        }

        Stream.Position = gemOffset;
        Stream.WriteByte((byte)gemFlag);
    }
}
