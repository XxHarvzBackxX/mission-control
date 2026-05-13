export type ReadinessState = 'Ready' | 'AtRisk' | 'NotReady';
export type MissionControlMode = 'Crewed' | 'Probe';
export type WarningType =
  | 'InsufficientDeltaV'
  | 'LowReserveMargin'
  | 'MissingRequiredField'
  | 'MissingCrew'
  | 'InvalidTimeRange'
  | 'AdvisoryEndTimeWithoutStart';

export interface Warning {
  type: WarningType;
  message: string;
  isBlocking: boolean;
}

export interface MissionListItem {
  id: string;
  name: string;
  readinessState: ReadinessState;
  controlMode: MissionControlMode;
  crewSummary: string | null;
  probeCoreValue: string | null;
  warnings: Warning[];
  assignedRocketId: string | null;
  rocketName: string | null;
}

export interface MissionCalculationProfileDto {
  launchBodyId: string;
  targetBodyId: string;
  profileType: string;
  targetOrbitAltitude: number;
  atmosphericEfficiencyMultiplier: number;
  safetyMarginPercent: number;
  requiredDeltaVOverride: number | null;
}

export interface RequiredDeltaVBreakdown {
  totalRequiredDeltaV: number;
  ascentDeltaV: number;
  transferDeltaV: number;
  descentDeltaV: number;
  returnDeltaV: number;
  estimationMethod: string;
  isApproximated: boolean;
}

export interface MissionSummary {
  id: string;
  name: string;
  targetBodyValue: string;
  targetBodyIsCustom: boolean;
  missionTypeValue: string;
  missionTypeIsCustom: boolean;
  availableDeltaV: number;
  requiredDeltaV: number;
  reserveMarginPercent: number;
  readinessState: ReadinessState;
  controlMode: MissionControlMode;
  crewMembers: string[];
  probeCoreValue: string | null;
  probeCoreIsCustom: boolean;
  startMissionTime: number | null;
  endMissionTime: number | null;
  warnings: Warning[];
  assignedRocketId: string | null;
  rocketName: string | null;
  calculationProfile: MissionCalculationProfileDto | null;
  requiredDeltaVBreakdown: RequiredDeltaVBreakdown | null;
}

export interface CreateMissionRequest {
  name: string;
  targetBodyValue: string;
  targetBodyIsCustom: boolean;
  missionTypeValue: string;
  missionTypeIsCustom: boolean;
  availableDeltaV: number;
  requiredDeltaV: number;
  controlMode: MissionControlMode;
  crewMembers: string[];
  probeCoreValue: string | null;
  probeCoreIsCustom: boolean;
  startMissionTime: number | null;
  endMissionTime: number | null;
  assignedRocketId: string | null;
  calculationProfile: MissionCalculationProfileDto | null;
}

export type UpdateMissionRequest = CreateMissionRequest;

export interface ReferenceData {
  targetBodies: string[];
  missionTypes: string[];
  probeCores: string[];
}
