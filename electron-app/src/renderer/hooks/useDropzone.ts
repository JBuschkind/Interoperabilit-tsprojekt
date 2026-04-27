import { useEffect, useRef, useState } from 'react';

type UseDropzoneProps = {
  value?: File | null;
  onChange?: (file: File | null) => void;
  maxSizeMB?: number;
};

export function useDropzone({
  value = null,
  onChange,
  maxSizeMB,
}: UseDropzoneProps) {
  const [file, setFile] = useState<File | null>(value);
  const [isDragging, setIsDragging] = useState(false);

  const inputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    setFile(value);
  }, [value]);

  const formatSize = (size?: number) => {
    if (!size) return '';
    return `${(size / 1024).toFixed(1)} KB`;
  };

  const validateFile = (f: File): boolean => {
    if (maxSizeMB && f.size > maxSizeMB * 1024 * 1024) {
      return false;
    }
    return true;
  };

  const handleFile = (f: File | null) => {
    if (!f) return;

    if (!validateFile(f)) return;

    setFile(f);
    onChange?.(f);

    if (inputRef.current) {
      inputRef.current.value = '';
    }
  };

  const handleDrop = (e: React.DragEvent<HTMLElement>) => {
    e.preventDefault();
    setIsDragging(false);

    const droppedFile = e.dataTransfer.files?.[0];
    handleFile(droppedFile);
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0] ?? null;
    handleFile(selected);
  };

  const clearFile = () => {
    setFile(null);
    onChange?.(null);

    if (inputRef.current) {
      inputRef.current.value = '';
    }
  };

  return {
    file,
    isDragging,
    inputRef,
    formatSize,
    setIsDragging,
    handleDrop,
    handleChange,
    clearFile,
  };
}
