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
  styles: [`
    .summary-container { max-width: 700px; margin: 0 auto; padding: 20px; }
    .readiness-badge { display: inline-block; padding: 6px 14px; border-radius: 4px; font-weight: 700; font-size: 1rem; }
    .readiness-badge.Ready { background: #d1fae5; color: #065f46; }
    .readiness-badge.AtRisk { background: #fef9c3; color: #854d0e; }
    .readiness-badge.NotReady { background: #fee2e2; color: #991b1b; }
    .detail-row { display: flex; padding: 8px 0; border-bottom: 1px solid #e5e7eb; }
    .detail-label { font-weight: 600; width: 200px; flex-shrink: 0; }
    .detail-value { flex: 1; }
    .warnings-list { margin-top: 12px; }
    .actions { margin-top: 20px; display: flex; gap: 12px; }
    .btn { padding: 8px 16px; border: none; border-radius: 4px; cursor: pointer; font-size: 0.9rem; text-decoration: none; }
    .btn-primary { background: #2563eb; color: white; }
    .btn-secondary { background: #e5e7eb; color: #374151; }
    .btn-danger { background: #ef4444; color: white; }
    h2 { margin-bottom: 20px; }
    .not-found { text-align: center; padding: 40px; color: #6b7280; }
  `]
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
