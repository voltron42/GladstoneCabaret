/** A thin adapter over the real file system, covered by external integration tests. */
import { link, mkdir, stat, unlink, writeFile } from "node:fs/promises";

export interface FileSystem {
  fileExists(path: string): Promise<boolean>;
  createDirectory(path: string): Promise<void>;
  writeBytes(path: string, content: Uint8Array): Promise<void>;
  moveWithoutReplacing(source: string, destination: string): Promise<void>;
  deleteFile(path: string): Promise<void>;
}

export const physicalFileSystem: FileSystem = {
  async fileExists(path) {
    try {
      return (await stat(path)).isFile();
    } catch {
      return false;
    }
  },
  async createDirectory(path) {
    await mkdir(path, { recursive: true });
  },
  async writeBytes(path, content) {
    await writeFile(path, content);
  },
  async moveWithoutReplacing(source, destination) {
    // A hard link fails if the destination exists; a rename would replace it.
    await link(source, destination);
    await unlink(source);
  },
  async deleteFile(path) {
    await unlink(path);
  },
};
