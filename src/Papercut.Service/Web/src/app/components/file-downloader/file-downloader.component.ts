import { Component, Injectable } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpEventType, HttpResponse } from '@angular/common/http';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { Observable, BehaviorSubject } from 'rxjs';
import { map } from 'rxjs/operators';

export interface DownloadProgress {
  id: string;
  filename: string;
  progress: number;
  status: 'downloading' | 'completed' | 'error';
  url?: string;
  buttonId?: string; // For tracking which button initiated the download
}

@Injectable({
  providedIn: 'root'
})
export class FileDownloaderService {
  private downloads$ = new BehaviorSubject<DownloadProgress[]>([]);
  private downloadingButtons$ = new BehaviorSubject<Set<string>>(new Set());
  private downloadId = 0;

  constructor(
    private http: HttpClient,
    private snackBar: MatSnackBar
  ) {}

  getDownloads(): Observable<DownloadProgress[]> {
    return this.downloads$.asObservable();
  }

  getDownloadingButtons(): Observable<Set<string>> {
    return this.downloadingButtons$.asObservable();
  }

  isButtonDownloading(buttonId: string): Observable<boolean> {
    return this.downloadingButtons$.pipe(
      map(buttons => buttons.has(buttonId))
    );
  }

  downloadFile(url: string, filename?: string, buttonId?: string): void {
    const id = `download-${++this.downloadId}`;
    const downloadFilename = filename || this.extractFilenameFromUrl(url);
    
    const download: DownloadProgress = {
      id,
      filename: downloadFilename,
      progress: 0,
      status: 'downloading',
      buttonId
    };

    // Add to downloads list
    const currentDownloads = this.downloads$.value;
    this.downloads$.next([...currentDownloads, download]);

    // Add button to downloading state
    if (buttonId) {
      const currentButtons = this.downloadingButtons$.value;
      currentButtons.add(buttonId);
      this.downloadingButtons$.next(new Set(currentButtons));
    }

    // Start download
    this.http.get(url, {
      responseType: 'blob',
      reportProgress: true,
      observe: 'events'
    }).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.DownloadProgress) {
          if (event.total) {
            const progress = Math.round(100 * event.loaded / event.total);
            this.updateDownloadProgress(id, progress);
          }
        } else if (event.type === HttpEventType.Response) {
          this.completeDownload(id, event as HttpResponse<Blob>, downloadFilename);
        }
      },
      error: (error) => {
        // Download failed - handled by error status
        this.updateDownloadStatus(id, 'error');
        this.removeButtonFromLoading(buttonId);
        this.snackBar.open(`Download failed: ${downloadFilename}`, 'Close', { duration: 5000 });
      }
    });
  }

  private updateDownloadProgress(id: string, progress: number): void {
    const downloads = this.downloads$.value;
    const index = downloads.findIndex(d => d.id === id);
    if (index !== -1) {
      downloads[index].progress = progress;
      this.downloads$.next([...downloads]);
    }
  }

  private updateDownloadStatus(id: string, status: DownloadProgress['status']): void {
    const downloads = this.downloads$.value;
    const index = downloads.findIndex(d => d.id === id);
    if (index !== -1) {
      downloads[index].status = status;
      this.downloads$.next([...downloads]);
    }
  }

  private completeDownload(id: string, response: HttpResponse<Blob>, filename: string): void {
    if (response.body) {
      // Create download URL and trigger download
      const blob = response.body;
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = filename;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);

      this.updateDownloadStatus(id, 'completed');
      
      // Remove button from loading state
      const download = this.downloads$.value.find(d => d.id === id);
      if (download?.buttonId) {
        this.removeButtonFromLoading(download.buttonId);
      }

      this.snackBar.open(`Downloaded: ${filename}`, 'Close', { duration: 3000 });

      // Remove completed download after 5 seconds
      setTimeout(() => {
        this.removeDownload(id);
      }, 5000);
    }
  }

  removeDownload(id: string): void {
    const downloads = this.downloads$.value.filter(d => d.id !== id);
    this.downloads$.next(downloads);
  }

  private removeButtonFromLoading(buttonId?: string): void {
    if (buttonId) {
      const currentButtons = this.downloadingButtons$.value;
      currentButtons.delete(buttonId);
      this.downloadingButtons$.next(new Set(currentButtons));
    }
  }

  private extractFilenameFromUrl(url: string): string {
    try {
      const urlParts = url.split('/');
      const lastPart = urlParts[urlParts.length - 1];
      return decodeURIComponent(lastPart) || 'download';
    } catch {
      return 'download';
    }
  }
}

@Component({
  selector: 'app-file-downloader',
  standalone: true,
  imports: [
    CommonModule,
    MatProgressBarModule,
    MatIconModule,
    MatButtonModule,
    MatSnackBarModule
  ],
  template: `
    <div *ngIf="downloads.length > 0" class="fixed bottom-4 right-4 z-50 space-y-2">
      <div *ngFor="let download of downloads" 
           class="bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 p-3 min-w-80 max-w-96">
        <div class="flex items-center gap-3">
          <mat-icon [ngClass]="{
            'text-blue-600': download.status === 'downloading',
            'text-green-600': download.status === 'completed',
            'text-red-600': download.status === 'error'
          }">
            {{ getStatusIcon(download.status) }}
          </mat-icon>
          
          <div class="flex-1 min-w-0">
            <div class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
              {{ download.filename }}
            </div>
            <div class="text-xs text-gray-600 dark:text-gray-400">
              {{ getStatusText(download) }}
            </div>
            
            <!-- Progress Bar -->
            <mat-progress-bar 
              *ngIf="download.status === 'downloading'"
              mode="determinate" 
              [value]="download.progress"
              class="mt-1">
            </mat-progress-bar>
          </div>
          
          <button mat-icon-button 
                  (click)="removeDownload(download.id)"
                  class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200">
            <mat-icon>close</mat-icon>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      position: fixed;
      pointer-events: none;
    }
    
    .fixed {
      pointer-events: auto;
    }
  `]
})
export class FileDownloaderComponent {
  downloads: DownloadProgress[] = [];

  constructor(private downloaderService: FileDownloaderService) {
    this.downloaderService.getDownloads().subscribe(downloads => {
      this.downloads = downloads;
    });
  }

  removeDownload(id: string): void {
    this.downloaderService.removeDownload(id);
  }

  getStatusIcon(status: DownloadProgress['status']): string {
    switch (status) {
      case 'downloading': return 'download';
      case 'completed': return 'check_circle';
      case 'error': return 'error';
      default: return 'download';
    }
  }

  getStatusText(download: DownloadProgress): string {
    switch (download.status) {
      case 'downloading': return `${download.progress}% downloading...`;
      case 'completed': return 'Download completed';
      case 'error': return 'Download failed';
      default: return '';
    }
  }
}
