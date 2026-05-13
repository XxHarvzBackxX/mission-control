import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CelestialBodyDto, CreateCustomBodyRequest } from '../models/celestial-body.model';

@Injectable({ providedIn: 'root' })
export class CelestialBodiesService {
  private readonly baseUrl = '/api/celestial-bodies';

  constructor(private http: HttpClient) {}

  getAll(): Observable<CelestialBodyDto[]> {
    return this.http.get<CelestialBodyDto[]>(this.baseUrl);
  }

  createCustom(request: CreateCustomBodyRequest): Observable<CelestialBodyDto> {
    return this.http.post<CelestialBodyDto>(`${this.baseUrl}/custom`, request);
  }
}
