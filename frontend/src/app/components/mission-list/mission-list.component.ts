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
  styleUrl: './mission-list.component.scss',
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
