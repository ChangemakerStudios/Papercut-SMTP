import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, shareReplay } from 'rxjs';
import { EnvironmentService } from './environment.service';

export interface McpStatus {
  enabled: boolean;
  url: string | null;
}

/**
 * Reports whether the service's MCP (Model Context Protocol) endpoint is
 * enabled, and its URL. Mirrors GET /api/mcp (McpController).
 */
@Injectable({ providedIn: 'root' })
export class McpService {
  /** MCP status, fetched once and replayed to all subscribers. */
  readonly status$: Observable<McpStatus>;

  constructor(http: HttpClient, environmentService: EnvironmentService) {
    this.status$ = http.get<McpStatus>(environmentService.getApiEndpoint('mcp')).pipe(
      catchError(() => of({ enabled: false, url: null })),
      shareReplay(1)
    );
  }
}
