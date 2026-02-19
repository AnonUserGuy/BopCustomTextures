using BopCustomTextures.Logging;
using System;
using System.IO;
using System.IO.Compression;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manages source files in custom mixtapes, including routines to load them and save them in future mixtapes.
/// </summary>
/// <param name="logger">Plugin-specific logger.</param>
/// <param name="tempPath">Where to temporarily save source files in custom mixtape while custom mixtape is loaded.</param>
public class CustomFileManager(ILogger logger, string tempPath) : BaseCustomManager(logger)
{
    public string tempPath = tempPath;
    public static FileStream tempLock = null;

    public bool WriteDirectory(string path)
    {
        if (tempLock != null)
        {
            var subpaths = Directory.EnumerateDirectories(tempPath);
            foreach (var subpath in subpaths)
            {
                if (CustomManager.IsCustomResourceDirectory(subpath) ||
                    CustomSceneManager.IsCustomSceneDirectory(subpath) ||
                    CustomTextureManager.IsCustomTextureDirectory(subpath)
                    )
                {
                    CopyDirectory(subpath, Path.Combine(path, subpath.Substring(tempPath.Length + 1)));
                }
            }
            return true;
        }
        return false;
    }

    public void CopyDirectory(string path, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var dir in Directory.EnumerateDirectories(path))
        {
            CopyDirectory(dir, Path.Combine(dest, dir.Substring(path.Length + 1)));
        }
        foreach (var file in Directory.EnumerateFiles(path))
        {
            File.Copy(file, Path.Combine(dest, file.Substring(path.Length + 1)));
        }
    }

    public void BackupDirectory(string path, string dest)
    {
        if (tempLock == null)
        {
            Directory.CreateDirectory(tempPath);
            tempLock = new FileStream(Path.Combine(tempPath, ".tmp"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        CopyDirectory(path, Path.Combine(tempPath, dest));
    }

    public void DeleteTempDirectory()
    {
        if (tempLock != null)
        {
            tempLock.Close();
            tempLock = null;
            Directory.Delete(tempPath, true);
        }
    }

    public string CreateUniqueTempDirectory(string prefix)
    {
        Directory.CreateDirectory(tempPath);
        string tempDirectoryPath = Path.Combine(tempPath, $"{prefix}_{Guid.NewGuid():N}");
        if (Directory.Exists(tempDirectoryPath))
        {
            Directory.Delete(tempDirectoryPath, true);
        }

        Directory.CreateDirectory(tempDirectoryPath);
        return tempDirectoryPath;
    }

    public string ExtractArchiveToTempDirectory(string archivePath, string prefix)
    {
        string workingPath = CreateUniqueTempDirectory(prefix);
        ZipFile.ExtractToDirectory(archivePath, workingPath);
        return workingPath;
    }

    public void PackDirectoryToArchive(string sourceDirectory, string archivePath)
    {
        var backupArchivePath = archivePath + ".bak";
        if (File.Exists(archivePath))
        {
            File.Copy(archivePath, backupArchivePath, true);
        }
        
        var tempArchivePath = Path.Combine(tempPath, $"{Path.GetFileNameWithoutExtension(archivePath)}_{Guid.NewGuid():N}.tmp");

        try
        {
            ZipFile.CreateFromDirectory(sourceDirectory, tempArchivePath, CompressionLevel.Optimal, false);
            File.Copy(tempArchivePath, archivePath, true);
        }
        catch (Exception)
        {
            logger.LogWarning($"Failed to pack directory ${archivePath} to {tempArchivePath}, restoring backup...");
            File.Copy(archivePath, tempArchivePath, true);
        }
        finally
        {
            if (File.Exists(tempArchivePath))
            {
                File.Delete(tempArchivePath);
            }

            if (File.Exists(backupArchivePath))
            {
                File.Delete(backupArchivePath);
            }
        }
    }

    public static void CleanUpTempDirectories(string tempParentPath)
    {
        if (!Directory.Exists(tempParentPath))
        {
            Directory.CreateDirectory(tempParentPath);
            return;
        }
        foreach (string otherTempPath in Directory.EnumerateDirectories(tempParentPath))
        {
            try
            {
                // check temp directory isn't being used by other Bits & Bops instance.
                LockFileLocked(Path.Combine(otherTempPath, ".tmp"));
                Directory.Delete(otherTempPath, true);
            }
            catch { }
        }
    }

    public static void LockFileLocked(string path)
    {
        // attempt to create a file to see if a temp directory is being used by another Bits & Bops instance. 
        using var _ = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    public static bool ShouldBackupDirectory()
    {
        return TempoSceneManager.GetActiveSceneKey() == SceneKey.MixtapeEditor;
    }
}
