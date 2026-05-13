export type PartCategory =
  | 'Pods'
  | 'FuelTanks'
  | 'Engines'
  | 'CommandAndControl'
  | 'Structural'
  | 'Coupling'
  | 'Payload'
  | 'Aerodynamics'
  | 'Ground'
  | 'Thermal'
  | 'Electrical'
  | 'Communication'
  | 'Science'
  | 'Cargo'
  | 'Utility';

export interface EngineStatsDto {
  thrustSeaLevel: number;
  thrustVacuum: number;
  ispSeaLevel: number;
  ispVacuum: number;
  fuelTypes: string[];
}

export interface PartDto {
  id: string;
  name: string;
  category: PartCategory;
  dryMass: number;
  wetMass: number;
  fuelCapacity: Record<string, number> | null;
  engineStats: EngineStatsDto | null;
}
