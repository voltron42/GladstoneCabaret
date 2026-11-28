namespace FetchLrclib;

public interface IFileSystem
{
    bool FileExists(string path);

    void CreateDirectory(string path);

    void WriteAllBytes(string path, byte[] content);

    void MoveWithoutReplacing(string source, string destination);

    void DeleteFile(string path);
}

/// <summary>A thin adapter over the real file system, covered by external integration tests.</summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void WriteAllBytes(string path, byte[] content) => File.WriteAllBytes(path, content);

    public void MoveWithoutReplacing(string source, string destination) =>
        File.Move(source, destination, overwrite: false);

    public void DeleteFile(string path) => File.Delete(path);
}
