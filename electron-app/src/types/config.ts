export type ConfigValue = string | number | boolean | null;

export type BaseConfigItem = {
  id: string;
  label: string;
  value: ConfigValue;
  defaultValue: ConfigValue;
};

export type TextConfigItem = BaseConfigItem & {
  type: 'text';
  placeholder?: string;
};

export type NumberConfigItem = BaseConfigItem & {
  type: 'number';
  placeholder?: string;
};

export type SelectConfigItem = BaseConfigItem & {
  type: 'select';
  options: { label: string; value: string }[];
};

export type TextAreaConfigItem = BaseConfigItem & {
  type: 'textarea';
  rows?: number;
  placeholder?: string;
};

export type ConfigItem =
  | TextConfigItem
  | NumberConfigItem
  | SelectConfigItem
  | TextAreaConfigItem;
