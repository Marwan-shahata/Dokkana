import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import { UserAdminDTO } from '../models/order.models';

// Admin user management (/api/users). Admin-only on the server.
@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/users`;

  getAll(): Observable<UserAdminDTO[]> {
    return this.http.get<ApiResult<UserAdminDTO[]>>(this.base).pipe(map(r => r.data ?? []));
  }

  setStatus(id: string, disabled: boolean): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.base}/${id}/status`, { disabled });
  }
}
