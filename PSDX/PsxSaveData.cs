namespace PSDX;

public class PsxSaveData
{
    private const int _saveDataLength = 8320; // 8192 (1 block) + 128 (header sector) bytes.

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
}
