import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MissionService } from '../../services/mission.service';
import { MissionListItem } from '../../models/mission.model';

@Component({
  selector: 'app-mission-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './mission-list.component.html',
  styles: [`
    .list-container { max-width: 800px; margin: 0 auto; padding: 20px; }
    .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: 10px 12px; text-align: left; border-bottom: 1px solid #e5e7eb; }
    th { font-weight: 600; background: #f9fafb; }
    tr:hover { background: #f3f4f6; }
    .readiness { font-weight: 600; }
    .readiness.Ready { color: #065f46; }
    .readiness.AtRisk { color: #854d0e; }
    .readiness.NotReady { color: #991b1b; }
    .btn { padding: 8px 16px; border: none; border-radius: 4px; cursor: pointer; font-size: 0.9rem; text-decoration: none; }
    .btn-primary { background: #2563eb; color: white; }
    .empty { text-align: center; padding: 40px; color: #6b7280; }
    a.mission-link { color: #2563eb; text-decoration: none; }
    a.mission-link:hover { text-decoration: underline; }
  `]
})
export class MissionListComponent implements OnInit {
  missions: MissionListItem[] = [];
  loading = true;

  constructor(private missionService: MissionService) {}

  ngOnInit(): void {
    this.missionService.getAll().subscribe({
      next: (missions) => {
        this.missions = missions;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  formatReadiness(state: string): string {
    if (state === 'AtRisk') return 'At Risk';
    if (state === 'NotReady') return 'Not Ready';
    return state;
  }
}
