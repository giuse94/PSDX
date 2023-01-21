using System;
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
        Stream.ReadExactly(buffer, 0, _fileNameLength);
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

    private const int _checksumOffset = 0x1A4;

    private const int _akuAkuOffset = 0x1B4;

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
        Stream.ReadExactly(bytes, 0, bytes.Length);
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
        Stream.ReadExactly(buffer, 0, buffer.Length);

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
        Stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Gets the number of Aku Aku masks currently stored in the save data file.
    /// </summary>
    public int GetAkuAkuMasks()
    {
        byte[] bytes = new byte[sizeof(int)];
        Stream.Position = _akuAkuOffset;
        Stream.ReadExactly(bytes, 0, bytes.Length);
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
        Stream.Write(bytes, 0, bytes.Length);
    }
}
