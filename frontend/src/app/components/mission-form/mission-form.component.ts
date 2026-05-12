import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MissionService } from '../../services/mission.service';
import {
  MissionControlMode,
  CreateMissionRequest,
  ReferenceData,
  MissionSummary,
} from '../../models/mission.model';

@Component({
  selector: 'app-mission-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './mission-form.component.html',
  styles: [`
    .form-container { max-width: 700px; margin: 0 auto; padding: 20px; }
    .form-group { margin-bottom: 16px; }
    label { display: block; font-weight: 600; margin-bottom: 4px; }
    input, select { width: 100%; padding: 8px; box-sizing: border-box; border: 1px solid #ccc; border-radius: 4px; }
    .crew-list { margin-top: 8px; }
    .crew-item { display: flex; gap: 8px; margin-bottom: 4px; align-items: center; }
    .crew-item input { flex: 1; }
    .btn { padding: 8px 16px; border: none; border-radius: 4px; cursor: pointer; font-size: 0.9rem; }
    .btn-primary { background: #2563eb; color: white; }
    .btn-primary:hover { background: #1d4ed8; }
    .btn-secondary { background: #e5e7eb; color: #374151; }
    .btn-danger { background: #ef4444; color: white; }
    .btn-small { padding: 4px 10px; font-size: 0.8rem; }
    .error-msg { color: #991b1b; background: #fee2e2; padding: 8px 12px; border-radius: 4px; margin-bottom: 12px; }
    .mode-toggle { display: flex; gap: 12px; margin-top: 4px; }
    .hidden { display: none; }
    h2 { margin-bottom: 20px; }
  `]
})
export class MissionFormComponent implements OnInit {
  isEditMode = false;
  missionId: string | null = null;

  name = '';
  targetBodyValue = '';
  targetBodyIsCustom = false;
  missionTypeValue = '';
  missionTypeIsCustom = false;
  availableDeltaV: number | null = null;
  requiredDeltaV: number | null = null;
  controlMode: MissionControlMode = 'Crewed';
  crewMembers: string[] = [''];
  probeCoreValue = '';
  probeCoreIsCustom = false;
  startMissionTime: number | null = null;
  endMissionTime: number | null = null;

  customTargetBody = '';
  customMissionType = '';
  customProbeCore = '';

  referenceData: ReferenceData | null = null;
  errorMessage: string | null = null;
  saving = false;

  constructor(
    private missionService: MissionService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.missionService.getReferenceData().subscribe(data => {
      this.referenceData = data;
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.missionId = id;
      this.missionService.getById(id).subscribe({
        next: (m) => this.populateForm(m),
        error: () => this.router.navigate(['/'])
      });
    }
  }

  private populateForm(m: MissionSummary): void {
    this.name = m.name;
    if (m.targetBodyIsCustom) {
      this.targetBodyValue = 'Other';
      this.customTargetBody = m.targetBodyValue;
      this.targetBodyIsCustom = true;
    } else {
      this.targetBodyValue = m.targetBodyValue;
    }
    if (m.missionTypeIsCustom) {
      this.missionTypeValue = 'Other';
      this.customMissionType = m.missionTypeValue;
      this.missionTypeIsCustom = true;
    } else {
      this.missionTypeValue = m.missionTypeValue;
    }
    this.availableDeltaV = m.availableDeltaV;
    this.requiredDeltaV = m.requiredDeltaV;
    this.controlMode = m.controlMode;
    this.crewMembers = m.crewMembers.length > 0 ? [...m.crewMembers] : [''];
    if (m.probeCoreValue) {
      if (m.probeCoreIsCustom) {
        this.probeCoreValue = 'Other';
        this.customProbeCore = m.probeCoreValue;
        this.probeCoreIsCustom = true;
      } else {
        this.probeCoreValue = m.probeCoreValue;
      }
    }
    this.startMissionTime = m.startMissionTime;
    this.endMissionTime = m.endMissionTime;
  }

  onTargetBodyChange(): void {
    this.targetBodyIsCustom = this.targetBodyValue === 'Other';
    if (!this.targetBodyIsCustom) this.customTargetBody = '';
  }

  onMissionTypeChange(): void {
    this.missionTypeIsCustom = this.missionTypeValue === 'Other';
    if (!this.missionTypeIsCustom) this.customMissionType = '';
  }

  onProbeCoreChange(): void {
    this.probeCoreIsCustom = this.probeCoreValue === 'Other';
    if (!this.probeCoreIsCustom) this.customProbeCore = '';
  }

  addCrewMember(): void {
    this.crewMembers.push('');
  }

  removeCrewMember(index: number): void {
    this.crewMembers.splice(index, 1);
  }

  trackByIndex(index: number): number {
    return index;
  }

  onSubmit(): void {
    this.errorMessage = null;
    this.saving = true;

    const request: CreateMissionRequest = {
      name: this.name,
      targetBodyValue: this.targetBodyIsCustom ? this.customTargetBody : this.targetBodyValue,
      targetBodyIsCustom: this.targetBodyIsCustom,
      missionTypeValue: this.missionTypeIsCustom ? this.customMissionType : this.missionTypeValue,
      missionTypeIsCustom: this.missionTypeIsCustom,
      availableDeltaV: this.availableDeltaV ?? 0,
      requiredDeltaV: this.requiredDeltaV ?? 0,
      controlMode: this.controlMode,
      crewMembers: this.controlMode === 'Crewed'
        ? this.crewMembers.filter(c => c.trim().length > 0)
        : [],
      probeCoreValue: this.controlMode === 'Probe'
        ? (this.probeCoreIsCustom ? this.customProbeCore : this.probeCoreValue) || null
        : null,
      probeCoreIsCustom: this.controlMode === 'Probe' ? this.probeCoreIsCustom : false,
      startMissionTime: this.startMissionTime,
      endMissionTime: this.endMissionTime
    };

    const obs = this.isEditMode
      ? this.missionService.update(this.missionId!, request)
      : this.missionService.create(request);

    obs.subscribe({
      next: (result) => {
        this.saving = false;
        this.router.navigate(['/missions', result.id]);
      },
      error: (err) => {
        this.saving = false;
        if (err.status === 409 || err.status === 400) {
          const body = err.error;
          if (body?.errors?.length) {
            this.errorMessage = body.errors.map((e: any) => e.message).join(' ');
          } else {
            this.errorMessage = 'Validation failed.';
          }
        } else {
          this.errorMessage = 'An unexpected error occurred.';
        }
      }
    });
  }
}
