import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { RocketsService } from '../../services/rockets.service';
import { RocketSummary } from '../../models/rocket.model';

@Component({
  selector: 'app-rocket-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './rocket-detail.component.html',
  styleUrl: './rocket-detail.component.scss',
})
export class RocketDetailComponent implements OnInit {
  rocket: RocketSummary | null = null;
  loading = true;
  error: string | null = null;
  expandedStages = new Set<number>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private rocketsService: RocketsService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.router.navigate(['/rockets']); return; }

    this.rocketsService.getById(id).subscribe({
      next: (rocket) => {
        this.rocket = rocket;
        this.loading = false;
      },
      error: () => {
        this.error = 'Rocket not found.';
        this.loading = false;
      }
    });
  }

  toggleStage(stageNumber: number): void {
    if (this.expandedStages.has(stageNumber)) {
      this.expandedStages.delete(stageNumber);
    } else {
      this.expandedStages.add(stageNumber);
    }
  }

  isStageExpanded(stageNumber: number): boolean {
    return this.expandedStages.has(stageNumber);
  }

  formatDeltaV(value: number): string {
    return `${Math.round(value).toLocaleString()} m/s`;
  }

  formatMass(value: number): string {
    return `${value.toFixed(3)} t`;
  }

  formatPercent(value: number): string {
    return `${(value * 100).toFixed(0)}%`;
  }

  delete(): void {
    if (!this.rocket) return;
    if (!confirm(`Delete rocket "${this.rocket.name}"?`)) return;
    this.rocketsService.delete(this.rocket.id).subscribe({
      next: () => this.router.navigate(['/rockets']),
      error: () => alert('Failed to delete rocket.')
    });
  }
}
