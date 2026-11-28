namespace FetchLrclib;

public static class FileNames
{
    private const string ReservedCharacters = "<>:\"/\\|?*";

    public static string BaseName(string artistName, string trackName)
    {
        var characters = $"{artistName} - {trackName}"
            .Select(character => character < ' ' || ReservedCharacters.Contains(character) ? '_' : character)
            .ToArray();
        return new string(characters).TrimEnd('.', ' ');
    }
}
