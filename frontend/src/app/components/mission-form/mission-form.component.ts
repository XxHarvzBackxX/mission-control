import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MissionService } from '../../services/mission.service';
import { KerbinTimeInputComponent } from '../shared/kerbin-time-input/kerbin-time-input.component';
import {
  MissionControlMode,
  CreateMissionRequest,
  ReferenceData,
  MissionSummary,
} from '../../models/mission.model';

@Component({
  selector: 'app-mission-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, KerbinTimeInputComponent],
  templateUrl: './mission-form.component.html',
  styleUrl: './mission-form.component.scss',
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
