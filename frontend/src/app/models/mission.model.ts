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
}

export type UpdateMissionRequest = CreateMissionRequest;

export interface ReferenceData {
  targetBodies: string[];
  missionTypes: string[];
  probeCores: string[];
}
