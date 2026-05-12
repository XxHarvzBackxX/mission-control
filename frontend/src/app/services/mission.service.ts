import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  MissionListItem,
  MissionSummary,
  CreateMissionRequest,
  UpdateMissionRequest,
  ReferenceData,
} from '../models/mission.model';

@Injectable({ providedIn: 'root' })
export class MissionService {
  private readonly baseUrl = '/api/missions';

  constructor(private http: HttpClient) {}

  getAll(): Observable<MissionListItem[]> {
    return this.http.get<MissionListItem[]>(this.baseUrl);
  }

  getById(id: string): Observable<MissionSummary> {
    return this.http.get<MissionSummary>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateMissionRequest): Observable<MissionSummary> {
    return this.http.post<MissionSummary>(this.baseUrl, request);
  }

  update(id: string, request: UpdateMissionRequest): Observable<MissionSummary> {
    return this.http.put<MissionSummary>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  getReferenceData(): Observable<ReferenceData> {
    return this.http.get<ReferenceData>(`${this.baseUrl}/reference-data`);
  }
}
