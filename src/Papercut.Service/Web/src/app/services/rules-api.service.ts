import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { EnvironmentService } from './environment.service';
import { RuleDto } from '../models/rule-dto';

/**
 * Rules management (GET/PUT api/rules). PUT replaces the whole rule set —
 * the same semantics as the desktop's IPComm rules sync.
 */
@Injectable({ providedIn: 'root' })
export class RulesApiService {
  private readonly baseUrl: string;

  constructor(private http: HttpClient, environmentService: EnvironmentService) {
    this.baseUrl = environmentService.getApiEndpoint('rules');
  }

  getRules(): Observable<RuleDto[]> {
    return this.http.get<RuleDto[]>(this.baseUrl);
  }

  updateRules(rules: RuleDto[]): Observable<RuleDto[]> {
    return this.http.put<RuleDto[]>(this.baseUrl, rules);
  }
}
