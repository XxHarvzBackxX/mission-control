import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MissionService } from '../../services/mission.service';
import { MissionSummary } from '../../models/mission.model';
import { WarningBadgeComponent } from '../shared/warning-badge/warning-badge.component';
import { KerbinTimePipe } from '../../pipes/kerbin-time.pipe';

@Component({
  selector: 'app-mission-summary',
  standalone: true,
  imports: [CommonModule, RouterLink, WarningBadgeComponent, KerbinTimePipe],
  templateUrl: './mission-summary.component.html',
  styleUrl: './mission-summary.component.scss',
})
export class MissionSummaryComponent implements OnInit {
  mission: MissionSummary | null = null;
  loading = true;
  notFound = false;

  constructor(
    private missionService: MissionService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/']);
      return;
    }

    this.missionService.getById(id).subscribe({
      next: (m) => {
        this.mission = m;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.notFound = true;
      }
    });
  }

  deleteMission(): void {
    if (!this.mission) return;
    if (!confirm(`Delete mission "${this.mission.name}"?`)) return;

    this.missionService.delete(this.mission.id).subscribe({
      next: () => this.router.navigate(['/']),
      error: () => alert('Failed to delete mission.')
    });
  }
}
