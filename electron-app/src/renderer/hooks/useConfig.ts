// useConfig.ts
import { useState } from 'react';
import type { ConfigItem, ConfigValue } from '../../types/config';

export function useConfig(initialConfig: ConfigItem[]) {
  const [config, setConfig] = useState<ConfigItem[]>(initialConfig);
  const [isModalOpen, setModalOpen] = useState(false);

  const updateValue = (id: string, value: ConfigValue) => {
    setConfig((prev) =>
      prev.map((item) => (item.id === id ? { ...item, value } : item)),
    );
  };

  const reset = () => {
    // setConfig(initialConfig);
  };

  /**
   * CLI-ready argument array:
   * ["--flag", "value", "--boolFlag"]
   */
  const getCLIArgs = () => {
    return config.flatMap((item) => {
      const key = `--${item.id}`;
      const value = item.value;

      if (value === null || value === undefined || value === '') {
        return [];
      }

      // boolean flags → --flag (only if true)
      if (typeof value === 'boolean') {
        return value ? [key] : [];
      }

      // everything else → --key value
      return [key, String(value)];
    });
  };

  const save = () => {
    // placeholder for persistence (electron, localStorage, etc.)
  };

  return {
    config,
    setConfig,
    updateValue,
    getCLIArgs,
    reset,
    save,
    isModalOpen,
    setModalOpen,
  };
}
