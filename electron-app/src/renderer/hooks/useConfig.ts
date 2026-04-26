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
    setConfig(initialConfig);
  };

  const getPayload = () => {
    return config.reduce(
      (acc, item) => {
        acc[item.id] = item.value;
        return acc;
      },
      {} as Record<string, ConfigValue>,
    );
  };

  const save = () => {
    // placeholder for persistence (electron, localStorage, etc.)
  };

  return {
    config,
    setConfig,
    updateValue,
    getPayload,
    reset,
    save,
    isModalOpen,
    setModalOpen,
  };
}
