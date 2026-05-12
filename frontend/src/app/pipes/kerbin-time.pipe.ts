import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'kerbinTime', standalone: true })
export class KerbinTimePipe implements PipeTransform {
  private static readonly SECONDS_PER_YEAR = 9_201_600;
  private static readonly SECONDS_PER_DAY = 21_600;
  private static readonly SECONDS_PER_HOUR = 3_600;
  private static readonly SECONDS_PER_MINUTE = 60;

  transform(totalSeconds: number | null | undefined): string {
    if (totalSeconds == null) return '';

    let remaining = totalSeconds;
    const years = Math.floor(remaining / KerbinTimePipe.SECONDS_PER_YEAR);
    remaining %= KerbinTimePipe.SECONDS_PER_YEAR;
    const days = Math.floor(remaining / KerbinTimePipe.SECONDS_PER_DAY);
    remaining %= KerbinTimePipe.SECONDS_PER_DAY;
    const hours = Math.floor(remaining / KerbinTimePipe.SECONDS_PER_HOUR);
    remaining %= KerbinTimePipe.SECONDS_PER_HOUR;
    const minutes = Math.floor(remaining / KerbinTimePipe.SECONDS_PER_MINUTE);
    const seconds = remaining % KerbinTimePipe.SECONDS_PER_MINUTE;

    return `${years}y, ${days}d, ${hours}h, ${minutes}m, ${seconds}s`;
  }
}
