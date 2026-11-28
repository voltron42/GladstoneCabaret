import { randomUUID } from "node:crypto";
import { join } from "node:path";
import type { FileSystem } from "./physicalFileSystem.ts";

export interface Store {
  /** Resolves true when saved, or false when an existing file was kept. */
  saveIfAbsent(fileName: string, content: Uint8Array): Promise<boolean>;
}

export function createLyricFileStore(fileSystem: FileSystem, outputDirectory: string): Store {
  return {
    async saveIfAbsent(fileName, content) {
      const destination = join(outputDirectory, fileName);
      if (await fileSystem.fileExists(destination)) {
        return false;
      }
      await fileSystem.createDirectory(outputDirectory);
      const temporary = join(outputDirectory, `.${randomUUID()}.tmp`);
      try {
        await fileSystem.writeBytes(temporary, content);
        await fileSystem.moveWithoutReplacing(temporary, destination);
        return true;
      } catch (error) {
        if (await fileSystem.fileExists(destination)) {
          return false;
        }
        throw error;
      } finally {
        if (await fileSystem.fileExists(temporary)) {
          await fileSystem.deleteFile(temporary);
        }
      }
    },
  };
}
