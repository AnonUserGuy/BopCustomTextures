using System;
using System.IO;
using static System.IO.Path;

namespace BopCustomTextures.Customs;

public class TempDirectory : IDisposable
{
    private readonly string _path;
    public string Path => _path;
    private readonly FileStream TempLock;

    public TempDirectory(string path)
    {
        _path = path;
        DeleteDirectory();
        Directory.CreateDirectory(Path);
        TempLock = Lock(Path);
    }

    public static implicit operator string(TempDirectory obj) => obj.Path;

    public void Dispose()
    {
        TempLock.Close();
        DeleteDirectory();
    }

    private void DeleteDirectory()
    {
        if (Directory.Exists(Path))
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch { }
        }
    }

    public static FileStream Lock(string dirPath)
    {
        return new FileStream(Combine(dirPath, ".tmp"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }
}
