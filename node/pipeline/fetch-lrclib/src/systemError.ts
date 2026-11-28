/** True for errors from the operating system, such as file access failures, and not for programming errors. */
export function isSystemError(error: unknown): error is NodeJS.ErrnoException {
  return error instanceof Error && "syscall" in error;
}
