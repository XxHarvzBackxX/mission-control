export type FuelType = 'LiquidFuelOxidizer' | 'SolidFuel' | 'MonoPropellant' | 'Xenon' | 'LiquidFuelOnly';

export interface StageEntry {
  partId: string;
  quantity: number;
}

export interface StageDto {
  id: string;
  stageNumber: number;
  name: string;
  isJettisoned: boolean;
  notes: string | null;
  parts: StageEntry[];
}

export interface StageDeltaVItem {
  stageNumber: number;
  stageName: string;
  wetMass: number;
  dryMass: number;
  ispUsed: number;
  rawDeltaV: number;
  efficiencyFactor: number;
  asparagusBonus: number;
  effectiveDeltaV: number;
  warnings: RocketWarning[];
}

export interface RocketWarning {
  type: string;
  message: string;
  isBlocking: boolean;
}

export interface RocketDeltaVBreakdown {
  totalEffectiveDeltaV: number;
  isValid: boolean;
  stages: StageDeltaVItem[];
  warnings: RocketWarning[];
}

export interface RocketListItem {
  id: string;
  name: string;
  description: string;
  stageCount: number;
  usesAsparagusStaging: boolean;
  totalEffectiveDeltaV: number | null;
  hasWarnings: boolean;
  isValid: boolean;
}

export interface RocketSummary {
  id: string;
  name: string;
  description: string;
  notes: string | null;
  usesAsparagusStaging: boolean;
  asparagusEfficiencyBonus: number;
  stages: StageDto[];
  deltaVBreakdown: RocketDeltaVBreakdown | null;
}

export interface CreateStageRequest {
  stageNumber: number;
  name: string;
  isJettisoned: boolean;
  notes: string | null;
  parts: StageEntry[];
}

export interface CreateRocketRequest {
  name: string;
  description: string;
  notes: string | null;
  usesAsparagusStaging: boolean;
  asparagusEfficiencyBonus: number;
  stages: CreateStageRequest[];
}

export type UpdateRocketRequest = CreateRocketRequest;
