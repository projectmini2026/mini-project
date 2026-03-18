 import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface User {
  name: string;
  role: string;
}

@Injectable({
  providedIn: 'root'
})
export class HomeService {

  private apiUrl = 'https://localhost:44336/api/home';

  constructor(private http: HttpClient) {}

  getModules(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/modules`);
  }

  getTeachers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/teachers`);
  }

  getUserDetails(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/userdetails`);
  }

}