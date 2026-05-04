import { useEffect, useMemo, useState } from 'react';
import type { ConfigItem, ConfigValue } from '../../types/config';

const store = window.electron.store;

type ConfigRecord = Record<string, ConfigValue>;

export function useConfig(configName: string) {
  const [baseConfig, setBaseConfig] = useState<ConfigItem[]>([]);
  const [draftConfig, setDraftConfig] = useState<ConfigItem[]>([]);
  const [isModalOpen, setModalOpen] = useState(false);

  // -----------------------------------------
  // LOAD (ONLY SOURCE OF TRUTH)
  // -----------------------------------------
  useEffect(() => {
    const load = async () => {
      const schemaJSON =
        await window.electron.ipcRenderer.readConfig(configName);

      const schema: ConfigItem[] = JSON.parse(schemaJSON);

      const stored: ConfigRecord =
        (await store.get(`config-${configName}`)) || {};

      const hydrated = schema.map((item) => ({
        ...item,
        value: stored[item.id] ?? item.defaultValue,
      }));

      setBaseConfig(hydrated);
    };

    load();
  }, [configName]);

  // -----------------------------------------
  // OPEN MODAL → CREATE SNAPSHOT
  // -----------------------------------------
  const openModal = () => {
    setDraftConfig(structuredClone(baseConfig));
    setModalOpen(true);
  };

  // -----------------------------------------
  // CLOSE MODAL → DISCARD
  // -----------------------------------------
  const closeModal = () => {
    setDraftConfig([]);
    setModalOpen(false);
  };

  // -----------------------------------------
  // UPDATE DRAFT ONLY
  // -----------------------------------------
  const updateValue = (id: string, value: ConfigValue) => {
    setDraftConfig((prev) =>
      prev.map((item) => (item.id === id ? { ...item, value } : item)),
    );
  };

  // -----------------------------------------
  // SAVE → PERSIST DRAFT
  // -----------------------------------------
  const save = async () => {
    const obj: ConfigRecord = Object.fromEntries(
      draftConfig.map((item) => [item.id, item.value]),
    );

    await store.set(`config-${configName}`, obj);

    setBaseConfig(structuredClone(draftConfig));
    setModalOpen(false);
  };

  // -----------------------------------------
  // RESET → REVERT TO SAVED BASE
  // -----------------------------------------
  const resetToSaved = () => {
    setDraftConfig(structuredClone(baseConfig));
  };

  // -----------------------------------------
  // RESET → DEFAULT VALUES
  // -----------------------------------------
  const resetToDefaults = () => {
    setDraftConfig(
      baseConfig.map((item) => ({
        ...item,
        value: item.defaultValue,
      })),
    );
  };

  // -----------------------------------------
  // (DERIVED) DIRTY CHECK
  // -----------------------------------------
  const hasUnsavedChanges = useMemo(() => {
    if (!draftConfig.length) return false;

    return draftConfig.some((item) => {
      const original = baseConfig.find((b) => b.id === item.id);

      const originalValue = original?.value ?? item.defaultValue;

      return item.value !== originalValue;
    });
  }, [draftConfig, baseConfig]);

  // -----------------------------------------
  // CLI ARGS
  // -----------------------------------------
  const getCLIArgs = () => {
    const source = isModalOpen ? draftConfig : baseConfig;

    return source.flatMap((item) => {
      const key = `--${item.id}`;
      const value = item.value;

      if (value === null || value === undefined || value === '') {
        return [];
      }

      if (typeof value === 'boolean') {
        return value ? [key] : [];
      }

      return [key, String(value)];
    });
  };

  // -----------------------------------------
  // EXPORT
  // -----------------------------------------
  return {
    draftConfig,
    updateValue,
    save,
    resetToSaved,
    resetToDefaults,
    getCLIArgs,
    isModalOpen,
    openModal,
    closeModal,
    hasUnsavedChanges,
  };
}
