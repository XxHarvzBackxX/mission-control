# Mission Readiness Planner

## Goal

Create a web application that allows users to define space missions and determine whether a mission is ready for launch.

## Functional Requirements

1. Users can create a mission.
2. A mission has:
   - name,
   - target body,
   - mission type,
   - available delta-v,
   - required delta-v.
3. The system calculates a readiness state:
   - Ready,
   - At Risk,
   - Not Ready.
4. The system generates warnings when:
   - available delta-v is lower than required delta-v,
   - delta-v margin is below 10%.

## Non-Functional Requirements

1. The frontend should be responsive AngularTS.
2. The backend should expose a .NET REST API.
3. Mission calculations should complete instantly.

## Acceptance Criteria

1. A mission with sufficient delta-v is marked Ready.
2. A mission with insufficient delta-v is marked Not Ready.
3. A mission with less than 10% reserve delta-v is marked At Risk.