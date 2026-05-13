import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { RocketsService } from '../../services/rockets.service';
import { RocketListItem } from '../../models/rocket.model';

@Component({
  selector: 'app-rocket-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './rocket-list.component.html',
  styleUrl: './rocket-list.component.scss',
})
export class RocketListComponent implements OnInit {
  rockets: RocketListItem[] = [];
  loading = true;

  constructor(private rocketsService: RocketsService) {}

  ngOnInit(): void {
    this.rocketsService.getAll().subscribe({
      next: (rockets) => {
        this.rockets = rockets;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  formatDeltaV(value: number | null): string {
    if (value === null) return '—';
    return `${Math.round(value).toLocaleString()} m/s`;
  }
}
