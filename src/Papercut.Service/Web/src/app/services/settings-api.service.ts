import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { EnvironmentService } from './environment.service';

export interface ServerSettings {
  smtpIP: string;
  smtpPort: number;
  mcpEnabled: boolean;
  availableIPs: string[];
}

export interface UpdateServerSettingsRequest {
  smtpIP?: string;
  smtpPort?: number;
  mcpEnabled?: boolean;
}

export interface UpdateServerSettingsResponse {
  smtpRebound: boolean;
  mcpRequiresRestart: boolean;
}

/**
 * Server-side settings (GET/PUT api/settings): SMTP binding and the MCP
 * toggle. SMTP changes apply live; MCP takes effect on service restart.
 */
@Injectable({ providedIn: 'root' })
export class SettingsApiService {
  private readonly baseUrl: string;

  constructor(private http: HttpClient, environmentService: EnvironmentService) {
    this.baseUrl = environmentService.getApiEndpoint('settings');
  }

  getSettings(): Observable<ServerSettings> {
    return this.http.get<ServerSettings>(this.baseUrl);
  }

  updateSettings(request: UpdateServerSettingsRequest): Observable<UpdateServerSettingsResponse> {
    return this.http.put<UpdateServerSettingsResponse>(this.baseUrl, request);
  }
}
