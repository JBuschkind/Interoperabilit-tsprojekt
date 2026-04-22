export {};

declare global {
  interface Window {
    electronApi: {
      getFilePath: (file: File) => string;
    };
  }
}
