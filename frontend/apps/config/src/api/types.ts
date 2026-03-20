export interface MenuItemConfig {
  id: string;
  label: string;
  icon: string;
  path?: string;
  permissions?: string[];
  children?: MenuItemConfig[];
}

export interface MenuConfig {
  menu: MenuItemConfig[];
}

export interface EntityFieldConfig {
  name: string;
  roles: string[];
}

export interface EntityConfig {
  name: string;
  fields: EntityFieldConfig[];
}

export interface EntitiesConfig {
  entities: EntityConfig[];
}

export interface UpstreamEntry {
  address: string;
  routes: string[];
}

export type UpstreamsMap = Record<string, UpstreamEntry>;

export interface UpstreamsConfig {
  upstreams: UpstreamsMap;
}
