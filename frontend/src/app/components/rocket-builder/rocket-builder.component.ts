import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { RocketsService } from '../../services/rockets.service';
import { PartsService } from '../../services/parts.service';
import { CreateRocketRequest, CreateStageRequest, RocketListItem } from '../../models/rocket.model';
import { PartDto, PartCategory } from '../../models/part.model';

@Component({
  selector: 'app-rocket-builder',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './rocket-builder.component.html',
  styleUrl: './rocket-builder.component.scss',
})
export class RocketBuilderComponent implements OnInit {
  editId: string | null = null;
  loading = false;
  saving = false;
  error: string | null = null;

  name = '';
  description = '';
  notes = '';
  usesAsparagusStaging = false;
  asparagusEfficiencyBonus = 0.08;

  stages: CreateStageRequest[] = [];

  // Part picker state
  allParts: PartDto[] = [];
  existingRockets: RocketListItem[] = [];
  filteredParts: PartDto[] = [];
  partSearch = '';
  selectedCategory: PartCategory | '' = '';
  activeStageIndex: number | null = null;

  categories: PartCategory[] = [
    'Pods', 'FuelTanks', 'Engines', 'CommandAndControl', 'Structural',
    'Coupling', 'Payload', 'Aerodynamics', 'Ground', 'Thermal',
    'Electrical', 'Communication', 'Science', 'Cargo', 'Utility'
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private rocketsService: RocketsService,
    private partsService: PartsService
  ) {}

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.partsService.getAll().subscribe(parts => {
      this.allParts = parts;
      this.filteredParts = parts;
    });
    this.rocketsService.getAll().subscribe(rockets => { this.existingRockets = rockets; });

    if (this.editId) {
      this.loading = true;
      this.rocketsService.getById(this.editId).subscribe({
        next: (rocket) => {
          this.name = rocket.name;
          this.description = rocket.description;
          this.notes = rocket.notes ?? '';
          this.usesAsparagusStaging = rocket.usesAsparagusStaging;
          this.asparagusEfficiencyBonus = rocket.asparagusEfficiencyBonus;
          this.stages = rocket.stages.map(s => ({
            stageNumber: s.stageNumber,
            name: s.name,
            isJettisoned: s.isJettisoned,
            notes: s.notes,
            parts: s.parts.map(p => ({ partId: p.partId, quantity: p.quantity }))
          }));
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load rocket.';
          this.loading = false;
        }
      });
    } else {
      this.addStage();
    }
  }

  addStage(): void {
    const nextNumber = this.stages.length > 0
      ? Math.max(...this.stages.map(s => s.stageNumber)) + 1
      : 1;
    this.stages.push({
      stageNumber: nextNumber,
      name: `Stage ${nextNumber}`,
      isJettisoned: nextNumber > 1,
      notes: null,
      parts: []
    });
    this.activeStageIndex = this.stages.length - 1;
  }

  removeStage(index: number): void {
    this.stages.splice(index, 1);
    if (this.activeStageIndex === index) {
      this.activeStageIndex = this.stages.length > 0 ? 0 : null;
    }
  }

  onAsparagusChange(): void {
    if (this.usesAsparagusStaging && this.asparagusEfficiencyBonus === 0) {
      this.asparagusEfficiencyBonus = 0.08;
    }
    if (!this.usesAsparagusStaging) {
      this.asparagusEfficiencyBonus = 0;
    }
  }

  filterParts(): void {
    this.filteredParts = this.allParts.filter(p => {
      const matchesSearch = !this.partSearch ||
        p.name.toLowerCase().includes(this.partSearch.toLowerCase()) ||
        p.id.toLowerCase().includes(this.partSearch.toLowerCase());
      const matchesCategory = !this.selectedCategory || p.category === this.selectedCategory;
      return matchesSearch && matchesCategory;
    });
  }

  addPartToStage(part: PartDto): void {
    if (this.activeStageIndex === null) return;
    const stage = this.stages[this.activeStageIndex];
    const existing = stage.parts.find(p => p.partId === part.id);
    if (existing) {
      existing.quantity++;
    } else {
      stage.parts.push({ partId: part.id, quantity: 1 });
    }
  }

  removePartFromStage(stageIndex: number, partIndex: number): void {
    this.stages[stageIndex].parts.splice(partIndex, 1);
  }

  getPartName(partId: string): string {
    return this.allParts.find(p => p.id === partId)?.name ?? partId;
  }

  save(): void {
    if (!this.name.trim() || !this.description.trim() || this.stages.length === 0) {
      this.error = 'Name, description, and at least one stage are required.';
      return;
    }

    // Client-side uniqueness check (T065)
    const nameTaken = this.existingRockets.some(r =>
      r.name.toLowerCase() === this.name.trim().toLowerCase() && r.id !== this.editId
    );
    if (nameTaken) {
      this.error = `A rocket named "${this.name.trim()}" already exists.`;
      return;
    }
    const request: CreateRocketRequest = {
      name: this.name.trim(),
      description: this.description.trim(),
      notes: this.notes.trim() || null,
      usesAsparagusStaging: this.usesAsparagusStaging,
      asparagusEfficiencyBonus: this.usesAsparagusStaging ? this.asparagusEfficiencyBonus : 0,
      stages: this.stages
    };

    this.saving = true;
    this.error = null;

    const obs = this.editId
      ? this.rocketsService.update(this.editId, request)
      : this.rocketsService.create(request);

    obs.subscribe({
      next: (rocket) => this.router.navigate(['/rockets', rocket.id]),
      error: (err) => {
        this.error = err?.error?.errors?.[0]?.message ?? 'Failed to save rocket.';
        this.saving = false;
      }
    });
  }
}
