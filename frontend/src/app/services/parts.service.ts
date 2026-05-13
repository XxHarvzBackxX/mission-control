import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PartDto, PartCategory } from '../models/part.model';

@Injectable({ providedIn: 'root' })
export class PartsService {
  private readonly baseUrl = '/api/parts';

  constructor(private http: HttpClient) {}

  getAll(category?: PartCategory, search?: string): Observable<PartDto[]> {
    let params = new HttpParams();
    if (category) params = params.set('category', category);
    if (search) params = params.set('search', search);
    return this.http.get<PartDto[]>(this.baseUrl, { params });
  }
}
