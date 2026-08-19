import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  LucideAngularModule,
  Image,
  FileText,
  FileType2,
  Sheet,
  Archive,
  Paperclip,
  ChevronUp,
  ChevronDown,
  Download,
  type LucideIconData
} from 'lucide-angular';
import { DetailDto, EmailSectionDto } from '../../models';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { MessageService } from '../../services/message.service';
import { FileDownloaderService } from '../file-downloader/file-downloader.component';
import { DownloadButtonDirective } from '../../directives/download-button.directive';
import { SafeIframeComponent } from '../safe-iframe/safe-iframe.component';

@Component({
  selector: 'app-message-sections',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    LucideAngularModule,
    DownloadButtonDirective,
    SafeIframeComponent,
    FileSizePipe
  ],
  template: `
    <!-- Sections List -->
    <div class="h-full overflow-auto bg-surface">
      <div class="p-4 space-y-3">
        <div *ngFor="let section of getMessageSections(); let i = index" class="section-item">
          <!-- Section Header -->
          <div class="section-header">
            <lucide-icon [img]="getSectionIcon(section.fileName || section.mediaType || 'Unknown')"
                         [size]="16" class="text-muted"></lucide-icon>
            <div class="flex-1 min-w-0">
              <div class="section-type truncate">
                {{ getSectionTitle(section) }}
                <span *ngIf="section.isAttachment" class="section-badge">attachment</span>
              </div>
              <div class="section-info truncate">
                {{ section.mediaType || 'unknown type' }}<ng-container *ngIf="section.size != null"> · {{ section.size | fileSize }}</ng-container>
              </div>
            </div>
            <div class="flex items-center gap-1">
              <!-- View Button (for text/plain and text/html without filename) -->
              <button
                *ngIf="shouldShowViewButton(section)"
                class="section-action-btn"
                (click)="toggleSectionView(section, i)"
                [title]="isViewingSection(i) ? 'Collapse content' : 'Expand content'">
                <lucide-icon [img]="isViewingSection(i) ? sectionIcons.ChevronUp : sectionIcons.ChevronDown" [size]="16"></lucide-icon>
              </button>
              <!-- Download Button -->
              <button
                *ngIf="shouldShowDownloadButton(section)"
                class="section-action-btn"
                [appDownloadButton]="getDownloadButtonId(section, i)"
                [downloadUrl]="buildSectionUrl(section, i)"
                [downloadFilename]="section.fileName || 'section-' + (section.id || i)"
                title="Download">
                <lucide-icon [img]="sectionIcons.Download" [size]="16"></lucide-icon>
              </button>
            </div>
          </div>

          <!-- Expanded Content Area -->
          <div *ngIf="isViewingSection(i)" class="border-t" style="border-color: var(--pc-border);">
            <div class="p-3" style="background: var(--pc-surface-2);">
              <div *ngIf="isSectionLoading(i)" class="flex items-center justify-center py-8">
                <mat-spinner diameter="32"></mat-spinner>
                <span class="ml-3 text-sm text-muted">Loading content...</span>
              </div>
              <div *ngIf="!isSectionLoading(i)" class="rounded border overflow-hidden" style="border-color: var(--pc-border); background: var(--pc-surface);">
                <app-safe-iframe
                  cssStyle="min-height: 200px; max-height: 400px;"
                  [content]="getSectionContentForViewing(i)">
                </app-safe-iframe>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      height: 100%;
    }
    
    iframe {
      border: none;
      background: white;
    }

    .section-header {
      display: flex;
      align-items: center;
      gap: 10px;
    }

    .section-action-btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 30px;
      height: 30px;
      border-radius: 6px;
      border: 1px solid transparent;
      background: transparent;
      color: var(--pc-muted);
      cursor: pointer;
      transition: background-color 0.12s ease, color 0.12s ease;
    }

    .section-action-btn:hover {
      background: var(--pc-hover);
      color: var(--pc-accent-text);
      border-color: var(--pc-border);
    }

    .section-badge {
      display: inline-block;
      margin-left: 6px;
      padding: 1px 8px;
      border-radius: 999px;
      border: 1px solid var(--pc-border);
      background: var(--pc-accent-soft);
      color: var(--pc-accent-text);
      font-size: 10px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      vertical-align: middle;
    }
  `]
})
export class MessageSectionsComponent {
  protected readonly sectionIcons = { ChevronUp, ChevronDown, Download };

  @Input() message: DetailDto | null = null;
  
  // Section viewing state
  viewingSectionIndex: number | null = null;
  sectionContents: Map<number, string> = new Map();
  loadingSections: Set<number> = new Set();

  constructor(
    private messageService: MessageService,
    private fileDownloader: FileDownloaderService
  ) {}



  getSectionTitle(section: EmailSectionDto): string {
    if (section.fileName) {
      return section.fileName;
    }

    // Inline body parts have no filename — give them a friendly name instead
    // of repeating the media type in both lines
    const mediaType = (section.mediaType || '').toLowerCase();
    if (mediaType === 'text/html') return 'HTML body';
    if (mediaType === 'text/plain') return 'Plain text body';
    if (mediaType.startsWith('image/')) return 'Inline image';

    return section.mediaType || 'Unknown section';
  }

  getMessageSections(): EmailSectionDto[] {
    if (!this.message || !this.message.sections || this.message.sections.length === 0) {
      return [];
    }
    return this.message.sections;
  }

  downloadSection(section: EmailSectionDto, index: number) {
    if (this.message?.id) {
      const url = this.buildSectionUrl(section, index);
      const filename = section.fileName || `section-${section.id || index}`;
      const buttonId = this.getDownloadButtonId(section, index);
      this.fileDownloader.downloadFile(url, filename, buttonId);
    }
  }

  getDownloadButtonId(section: EmailSectionDto, index: number): string {
    return `download-section-${section.id || index}`;
  }

  toggleSectionView(section: EmailSectionDto, index: number) {
    if (this.isViewingSection(index)) {
      // Close the section
      this.viewingSectionIndex = null;
    } else {
      // Open the section
      this.viewingSectionIndex = index;
      
      if (!this.sectionContents.has(index)) {
        // Load content if not already loaded
        this.loadSectionContent(section, index);
      }
    }
  }

  private loadSectionContent(section: EmailSectionDto, index: number) {
    if (!this.message?.id) {
      return;
    }
    
    this.loadingSections.add(index);
    
    // Use different endpoints based on whether section has an ID
    const observable = section.id 
      ? this.messageService.getSectionContent(this.message.id, section.id)
      : this.messageService.getSectionByIndex(this.message.id, index);
    
    observable.subscribe({
      next: (content: string) => {
        this.sectionContents.set(index, content);
        this.loadingSections.delete(index);
      },
      error: (error: any) => {
        this.sectionContents.set(index, `<html><body><h2>Error loading section content</h2><p>${error.message || error}</p></body></html>`);
        this.loadingSections.delete(index);
      }
    });
  }

  isViewingSection(index: number): boolean {
    return this.viewingSectionIndex === index;
  }

  isSectionLoading(index: number): boolean {
    return this.loadingSections.has(index);
  }

  shouldShowViewButton(section: EmailSectionDto): boolean {
    const mediaType = (section.mediaType || '').toLowerCase();
    const hasFileName = !!section.fileName;
    
    // If it has a filename, always download
    if (hasFileName) {
      return false;
    }
    
    // If content type is text/plain or text/html, show view option
    const showView = mediaType === 'text/plain' || mediaType === 'text/html';
    return showView;
  }

  shouldShowDownloadButton(section: EmailSectionDto): boolean {
    const mediaType = (section.mediaType || '').toLowerCase();
    const hasFileName = !!section.fileName;
    
    // If it has a filename, always show download
    if (hasFileName) {
      return true;
    }
    
    // Don't show download button for text/plain or text/html (they have view button instead)
    const showDownload = !(mediaType === 'text/plain' || mediaType === 'text/html');
    return showDownload;
  }

  getSectionContentForViewing(index: number): string {
    const content = this.sectionContents.get(index);
    const sections = this.getMessageSections();
    const section = sections[index];
    const mediaType = section?.mediaType || '';
    const messageId = this.message?.id || '';
    
    // Use the message service's shared formatMessageContent method for consistent styling
    const formattedContent = this.messageService.formatMessageContent(content || '', mediaType, messageId);
        
    return formattedContent;
  }

  getSectionIcon(type: string): LucideIconData {
    const lowerType = type.toLowerCase();

    if (lowerType.includes('image') || lowerType.includes('.jpg') || lowerType.includes('.png') || lowerType.includes('.gif')) {
      return Image;
    } else if (lowerType.includes('text') || lowerType.includes('.txt')) {
      return FileText;
    } else if (lowerType.includes('pdf')) {
      return FileType2;
    } else if (lowerType.includes('word') || lowerType.includes('document') || lowerType.includes('.doc')) {
      return FileText;
    } else if (lowerType.includes('spreadsheet') || lowerType.includes('excel') || lowerType.includes('.xls')) {
      return Sheet;
    } else if (lowerType.includes('zip') || lowerType.includes('archive') || lowerType.includes('.zip')) {
      return Archive;
    } else {
      return Paperclip;
    }
  }

  buildSectionUrl(section: EmailSectionDto, index: number): string {
    const encodedMessageId = encodeURIComponent(this.message!.id!);
    
    if (section.id) {
      // Use content ID endpoint
      const encodedContentId = encodeURIComponent(section.id);
      return `/api/messages/${encodedMessageId}/contents/${encodedContentId}`;
    } else {
      // Use section index endpoint
      return `/api/messages/${encodedMessageId}/sections/${index}`;
    }
  }
}
