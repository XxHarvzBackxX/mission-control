import { Component, Input } from '@angular/core';
import { Warning } from '../../../models/mission.model';

@Component({
  selector: 'app-warning-badge',
  standalone: true,
  templateUrl: './warning-badge.component.html',
  styles: [`
    .warning-badge {
      display: inline-block;
      padding: 4px 10px;
      border-radius: 4px;
      font-size: 0.85rem;
      font-weight: 500;
      margin: 2px 4px 2px 0;
    }
    .warning-badge.blocking {
      background-color: #fee2e2;
      color: #991b1b;
      border: 1px solid #fca5a5;
    }
    .warning-badge.advisory {
      background-color: #fef9c3;
      color: #854d0e;
      border: 1px solid #fde047;
    }
  `]
})
export class WarningBadgeComponent {
  @Input({ required: true }) warning!: Warning;
}
