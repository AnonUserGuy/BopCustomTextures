using BopCustomTextures.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manages source files in custom mixtapes, including routines to load them and save them in future mixtapes.
/// </summary>
/// <param name="logger">Plugin-specific logger.</param>
/// <param name="tempPath">Where to temporarily save source files in custom mixtape while custom mixtape is loaded.</param>
public class CustomFileManager(ILogger logger, string tempPath) : BaseCustomManager(logger)
{
    public string TempPath = tempPath;
    public TempDirectory TempDirectory = null;

    public bool WriteDirectory(string path)
    {
        if (TempDirectory != null)
        {
            var subpaths = Directory.EnumerateDirectories(TempPath);
            foreach (var subpath in subpaths)
            {
                if (CustomManager.IsCustomResourceDirectory(subpath)
                    || CustomSceneManager.IsCustomSceneDirectory(subpath)
                    || CustomTextureManager.IsCustomTextureDirectory(subpath))
                {
                    CopyDirectory(subpath, Path.Combine(path, subpath.Substring(TempPath.Length + 1)));
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
        CheckLock();
        CopyDirectory(path, Path.Combine(TempPath, dest));
    }

    public int BackupFiles(IEnumerable<string> files, string parentPath)
    {
        CheckLock();
        int found = 0;
        foreach (var file in files)
        {
            found++;
            string srcPath = Path.Combine(parentPath, file);
            if (!File.Exists(srcPath))
            {
                Logger.LogError($"File not found: {file}");
                continue;
            }
            string destPath = Path.Combine(TempPath, file);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            File.Copy(srcPath, destPath);
        }
        return found;
    }

    private void CheckLock()
    {
        TempDirectory ??= new(TempPath);
    }

    public void DeleteTempDirectory()
    {
        if (TempDirectory != null)
        {
            TempDirectory.Dispose();
            TempDirectory = null;
        }
    }

    private TempDirectory CreateUniqueTempDirectory()
    {
        string tempDirectoryPath = $"{TempPath}_{Guid.NewGuid():N}";
        TempDirectory tempDirectory = new(tempDirectoryPath);
        return tempDirectory;
    }

    public TempDirectory ExtractArchiveToTempDirectory(string archivePath)
    {
        TempDirectory tempDirectory = CreateUniqueTempDirectory();
        ZipFile.ExtractToDirectory(archivePath, tempDirectory);
        return tempDirectory;
    }

    public void PackDirectoryToArchive(string sourceDirectory, string archivePath)
    {
        var backupArchivePath = archivePath + ".bak";
        if (File.Exists(archivePath))
        {
            File.Copy(archivePath, backupArchivePath, true);
        }
        
        var tempArchivePath = Path.Combine(TempPath, $"{Path.GetFileNameWithoutExtension(archivePath)}_{Guid.NewGuid():N}.tmp");

        try
        {
            ZipFile.CreateFromDirectory(sourceDirectory, tempArchivePath, CompressionLevel.Optimal, false);
            File.Copy(tempArchivePath, archivePath, true);
        }
        catch (Exception)
        {
            Logger.LogWarning($"Failed to pack directory ${archivePath} to {tempArchivePath}, restoring backup...");
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
                FileStream otherTempLock = TempDirectory.Lock(otherTempPath);
                otherTempLock.Dispose();
                Directory.Delete(otherTempPath, true);
            }
            catch { }
        }
    }

    public static bool ShouldBackupDirectory()
    {
        return TempoSceneManager.GetActiveSceneKey() == SceneKey.MixtapeEditor;
    }
}
