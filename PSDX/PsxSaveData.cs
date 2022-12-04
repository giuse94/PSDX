namespace PSDX;

public class PsxSaveData
{
    private const int _saveDataLength = 8320; // 8192 (1 block) + 128 (header sector) bytes.

    private const int _fileNameOffset = 0xA;

    private const int _fileNameLength = 20; // 20 bytes, discarding the null-terminator.

    private MemoryStream _stream = new(_saveDataLength);

    // Return a copy so that consumers won't interfere.
    private MemoryStream Stream => new MemoryStream(_stream.ToArray());

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
    private const string serialNumber = "BESCES-00967"; // Only the European version is currently supported.

    public CrashBandicoot2SaveData(Stream s) : base(s)
    {
        if (!GetFileName().StartsWith(serialNumber))
        {
            throw new ArgumentException("Only the European version of Crash Bandicoot 2 is currently supported.", nameof(s));
        }
    }
}
