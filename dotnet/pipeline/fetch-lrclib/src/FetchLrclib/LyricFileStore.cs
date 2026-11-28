namespace FetchLrclib;

public interface ILyricFileStore
{
    /// <summary>Returns true when saved, or false when an existing file was kept.</summary>
    bool SaveIfAbsent(string fileName, byte[] content);
}

public sealed class LyricFileStore(IFileSystem fileSystem, string outputDirectory) : ILyricFileStore
{
    public bool SaveIfAbsent(string fileName, byte[] content)
    {
        var destination = Path.Combine(outputDirectory, fileName);
        if (fileSystem.FileExists(destination))
        {
            return false;
        }

        fileSystem.CreateDirectory(outputDirectory);
        var temporary = Path.Combine(outputDirectory, $".{Guid.NewGuid():N}.tmp");
        try
        {
            fileSystem.WriteAllBytes(temporary, content);
            fileSystem.MoveWithoutReplacing(temporary, destination);
            return true;
        }
        catch (IOException) when (fileSystem.FileExists(destination))
        {
            return false;
        }
        finally
        {
            if (fileSystem.FileExists(temporary))
            {
                fileSystem.DeleteFile(temporary);
            }
        }
    }
}
