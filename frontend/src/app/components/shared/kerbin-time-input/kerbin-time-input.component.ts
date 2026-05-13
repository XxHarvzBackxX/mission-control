import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

/** Sub-field state (null = not set) */
export interface KerbinTimeFields {
  years:   number | null;
  days:    number | null;
  hours:   number | null;
  minutes: number | null;
  seconds: number | null;
}

@Component({
  selector: 'app-kerbin-time-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './kerbin-time-input.component.html',
  styleUrl: './kerbin-time-input.component.scss',
})
export class KerbinTimeInputComponent implements OnChanges {
  @Input() value: number | null = null;
  @Input() label: string = 'Time';
  @Output() valueChange = new EventEmitter<number | null>();

  // Kerbin calendar constants
  private readonly SECONDS_PER_MINUTE = 60;
  private readonly SECONDS_PER_HOUR   = 3_600;
  private readonly SECONDS_PER_DAY    = 21_600;
  private readonly SECONDS_PER_YEAR   = 9_201_600;

  // Calendar bounds for clamping
  private readonly MAX_DAYS    = 425; // 426 days per year (0-indexed: 0..425)
  private readonly MAX_HOURS   = 5;   // 6 hours per day (0-indexed: 0..5)
  private readonly MAX_MINUTES = 59;
  private readonly MAX_SECONDS = 59;

  fields: KerbinTimeFields = {
    years:   null,
    days:    null,
    hours:   null,
    minutes: null,
    seconds: null,
  };

  ngOnChanges(_changes: SimpleChanges | Record<string, unknown>): void {
    if (this.value === null || this.value === 0) {
      this.fields = { years: null, days: null, hours: null, minutes: null, seconds: null };
      return;
    }

    let remaining = Math.floor(this.value);
    const years   = Math.floor(remaining / this.SECONDS_PER_YEAR);
    remaining -= years * this.SECONDS_PER_YEAR;
    const days    = Math.floor(remaining / this.SECONDS_PER_DAY);
    remaining -= days * this.SECONDS_PER_DAY;
    const hours   = Math.floor(remaining / this.SECONDS_PER_HOUR);
    remaining -= hours * this.SECONDS_PER_HOUR;
    const minutes = Math.floor(remaining / this.SECONDS_PER_MINUTE);
    remaining -= minutes * this.SECONDS_PER_MINUTE;
    const seconds = remaining;

    this.fields = { years, days, hours, minutes, seconds };
  }

  onFieldChange(): void {
    // Clamp sub-fields to Kerbin calendar bounds
    if (this.fields.days    !== null) this.fields.days    = Math.min(this.fields.days,    this.MAX_DAYS);
    if (this.fields.hours   !== null) this.fields.hours   = Math.min(this.fields.hours,   this.MAX_HOURS);
    if (this.fields.minutes !== null) this.fields.minutes = Math.min(this.fields.minutes, this.MAX_MINUTES);
    if (this.fields.seconds !== null) this.fields.seconds = Math.min(this.fields.seconds, this.MAX_SECONDS);

    // Clamp negatives to zero
    if (this.fields.years   !== null && this.fields.years   < 0) this.fields.years   = 0;
    if (this.fields.days    !== null && this.fields.days    < 0) this.fields.days    = 0;
    if (this.fields.hours   !== null && this.fields.hours   < 0) this.fields.hours   = 0;
    if (this.fields.minutes !== null && this.fields.minutes < 0) this.fields.minutes = 0;
    if (this.fields.seconds !== null && this.fields.seconds < 0) this.fields.seconds = 0;

    const allNull =
      this.fields.years   === null &&
      this.fields.days    === null &&
      this.fields.hours   === null &&
      this.fields.minutes === null &&
      this.fields.seconds === null;

    if (allNull) {
      this.valueChange.emit(null);
      return;
    }

    const total =
      (this.fields.years   ?? 0) * this.SECONDS_PER_YEAR +
      (this.fields.days    ?? 0) * this.SECONDS_PER_DAY  +
      (this.fields.hours   ?? 0) * this.SECONDS_PER_HOUR +
      (this.fields.minutes ?? 0) * this.SECONDS_PER_MINUTE +
      (this.fields.seconds ?? 0);

    this.valueChange.emit(total > 0 ? total : null);
  }
}
