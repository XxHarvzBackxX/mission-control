import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RocketListItem, RocketSummary, CreateRocketRequest, UpdateRocketRequest } from '../models/rocket.model';

@Injectable({ providedIn: 'root' })
export class RocketsService {
  private readonly baseUrl = '/api/rockets';

  constructor(private http: HttpClient) {}

  getAll(): Observable<RocketListItem[]> {
    return this.http.get<RocketListItem[]>(this.baseUrl);
  }

  getById(id: string): Observable<RocketSummary> {
    return this.http.get<RocketSummary>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateRocketRequest): Observable<RocketSummary> {
    return this.http.post<RocketSummary>(this.baseUrl, request);
  }

  update(id: string, request: UpdateRocketRequest): Observable<RocketSummary> {
    return this.http.put<RocketSummary>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<{ affectedMissionCount: number }> {
    return this.http.delete<{ affectedMissionCount: number }>(`${this.baseUrl}/${id}`);
  }
}
