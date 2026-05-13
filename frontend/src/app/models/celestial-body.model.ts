export interface CelestialBodyDto {
  id: string;
  name: string;
  parentBodyId: string | null;
  equatorialRadius: number;
  surfaceGravity: number;
  surfacePressure: number;
  atmosphereHeight: number;
  sphereOfInfluence: number | null;
  semiMajorAxis: number | null;
  defaultOrbitAltitude: number;
  hasAtmosphere: boolean;
  isCustom: boolean;
}

export interface CreateCustomBodyRequest {
  name: string;
  parentBodyId: string | null;
  equatorialRadius: number;
  surfaceGravity: number;
  surfacePressure: number;
  atmosphereHeight: number;
  defaultOrbitAltitude: number;
}
