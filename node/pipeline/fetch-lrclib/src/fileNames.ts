const RESERVED_CHARACTERS = '<>:"/\\|?*';

export function baseName(artistName: string, trackName: string): string {
  return Array.from(`${artistName} - ${trackName}`, (character) =>
    character < " " || RESERVED_CHARACTERS.includes(character) ? "_" : character,
  )
    .join("")
    .replace(/[. ]+$/, "");
}
