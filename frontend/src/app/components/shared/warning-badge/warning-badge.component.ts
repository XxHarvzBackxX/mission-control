import { Component, Input } from '@angular/core';
import { Warning } from '../../../models/mission.model';

@Component({
  selector: 'app-warning-badge',
  standalone: true,
  templateUrl: './warning-badge.component.html',
  styleUrl: './warning-badge.component.scss',
})
export class WarningBadgeComponent {
  @Input({ required: true }) warning!: Warning;
}
