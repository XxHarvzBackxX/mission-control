import { ComponentFixture, TestBed } from '@angular/core/testing';
import { KerbinTimeInputComponent } from './kerbin-time-input.component';

describe('KerbinTimeInputComponent', () => {
  let component: KerbinTimeInputComponent;
  let fixture: ComponentFixture<KerbinTimeInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KerbinTimeInputComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(KerbinTimeInputComponent);
    component = fixture.componentInstance;
  });

  // ─── Decomposition ────────────────────────────────────────────────────────

  it('T19-1: decomposes total seconds into Y/D/H/M/S sub-fields', () => {
    // 1 Year (9201600s) + 1 Day (21600s) + 2 Hours (7200s) + 3 Minutes (180s) + 4 Seconds
    const total = 9201600 + 21600 + 7200 + 180 + 4;
    component.value = total;
    component.ngOnChanges({});
    expect(component.fields.years).toBe(1);
    expect(component.fields.days).toBe(1);
    expect(component.fields.hours).toBe(2);
    expect(component.fields.minutes).toBe(3);
    expect(component.fields.seconds).toBe(4);
  });

  it('T19-2: null value sets all fields to null (not-set state)', () => {
    component.value = null;
    component.ngOnChanges({});
    expect(component.fields.years).toBeNull();
    expect(component.fields.days).toBeNull();
    expect(component.fields.hours).toBeNull();
    expect(component.fields.minutes).toBeNull();
    expect(component.fields.seconds).toBeNull();
  });

  it('T19-3: zero value sets all fields to null (not-set state)', () => {
    component.value = 0;
    component.ngOnChanges({});
    expect(component.fields.years).toBeNull();
    expect(component.fields.days).toBeNull();
    expect(component.fields.hours).toBeNull();
    expect(component.fields.minutes).toBeNull();
    expect(component.fields.seconds).toBeNull();
  });

  // ─── Composition ─────────────────────────────────────────────────────────

  it('T19-4: composes sub-fields back to total seconds on change', () => {
    component.value = null;
    component.ngOnChanges({});

    let emitted: number | null | undefined;
    component.valueChange.subscribe((v) => (emitted = v));

    component.fields.years = 2;
    component.fields.days = 5;
    component.fields.hours = 3;
    component.fields.minutes = 10;
    component.fields.seconds = 30;
    component.onFieldChange();

    // 2*9201600 + 5*21600 + 3*3600 + 10*60 + 30
    const expected = 2 * 9201600 + 5 * 21600 + 3 * 3600 + 10 * 60 + 30;
    expect(emitted).toBe(expected);
  });

  // ─── Clamping ─────────────────────────────────────────────────────────────

  it('T19-5: clamps days to max 425', () => {
    component.value = null;
    component.ngOnChanges({});
    component.fields.years = 0;
    component.fields.days = 999;
    component.fields.hours = 0;
    component.fields.minutes = 0;
    component.fields.seconds = 0;
    component.onFieldChange();
    expect(component.fields.days).toBe(425);
  });

  it('T19-6: clamps hours to max 5', () => {
    component.value = null;
    component.ngOnChanges({});
    component.fields.years = 0;
    component.fields.days = 0;
    component.fields.hours = 99;
    component.fields.minutes = 0;
    component.fields.seconds = 0;
    component.onFieldChange();
    expect(component.fields.hours).toBe(5);
  });

  it('T19-7: all null fields emit null', () => {
    component.value = null;
    component.ngOnChanges({});

    let emitted: number | null | undefined;
    component.valueChange.subscribe((v) => (emitted = v));

    component.onFieldChange();
    expect(emitted).toBeNull();
  });

  // ─── Round-trip ──────────────────────────────────────────────────────────

  it('T19-8: round-trip: decompose then compose returns same seconds', () => {
    const original = 1 * 9201600 + 3 * 21600 + 4 * 3600 + 22 * 60 + 15;
    component.value = original;
    component.ngOnChanges({});

    let emitted: number | null | undefined;
    component.valueChange.subscribe((v) => (emitted = v));

    component.onFieldChange();
    expect(emitted).toBe(original);
  });
});
