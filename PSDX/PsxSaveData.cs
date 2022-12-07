namespace PSDX;

public class PsxSaveData
{
    private const int _saveDataLength = 8320; // 8192 (1 block) + 128 (header sector) bytes.

    private const int _fileNameOffset = 0xA;

    private const int _fileNameLength = 20; // 20 bytes, discarding the null-terminator.

    protected MemoryStream _stream = new(_saveDataLength);

    // Return a copy so that consumers won't interfere.
    public MemoryStream Stream => new MemoryStream(_stream.ToArray());

    public PsxSaveData(Stream s)
    {
        if (s == null) throw new ArgumentNullException(nameof(s));
        if (s.Length != _saveDataLength)
        {
            throw new ArgumentException($"The size of Single Save Format files (.MCS) must be {_saveDataLength} bytes.", nameof(s));
        }
        // Copy the stream so that we won't change the original data.
        s.CopyTo(_stream);
    }

    public string GetFileName()
    {
        _stream.Position = _fileNameOffset;
        byte[] buffer = new byte[_fileNameLength];
        _stream.Read(buffer, 0, _fileNameLength);
        return System.Text.Encoding.ASCII.GetString(buffer);
    }
}

public class CrashBandicoot2SaveData : PsxSaveData
{
    private const string _serialNumber = "BESCES-00967"; // Only the European version is currently supported.

    private const int _checksumOffset = 0x1A4;

    private const int _akuAkuOffset = 0x1B4;

    public CrashBandicoot2SaveData(Stream s) : base(s)
    {
        if (!GetFileName().StartsWith(_serialNumber))
        {
            throw new ArgumentException("Only the European version of Crash Bandicoot 2 is currently supported.", nameof(s));
        }
    }

    // Thanks to https://github.com/socram8888/tonyhax/blob/9d57fd2e072a4fd173218c321520051479d14012/entrypoints/fix-crash-checksum.sh
    public uint GetChecksum()
    {
        byte[] buffer = new byte[0x2A4 * 4];
        _stream.Position = 0x180;
        _stream.Read(buffer, 0, buffer.Length);

        uint checksum = 0x12345678;
        for (int i = 0; i < buffer.Length; i += 4)
        {
            // Skip the location where the checksum itself is stored.
            if (i == 36) i += 4; // Tenth word in the buffer, so index is 4 * (10 - 1).

            byte[] wordBytes = buffer[i..(i + 4)];
            uint word = BitConverter.ToUInt32(wordBytes, 0);
            checksum = (checksum + word) & 0xFFFFFFFF;
        }

        return checksum;
    }

    public void SetChecksum(uint checksum)
    {
        _stream.Position = _checksumOffset;
        byte[] bytes = BitConverter.GetBytes(checksum);
        _stream.Write(bytes, 0, bytes.Length);
    }

    public int GetAkuAkuMasks()
    {
        _stream.Position = _akuAkuOffset;
        return _stream.ReadByte();
    }
}
