import { Component, Input, AfterViewInit, ViewChildren, QueryList, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DetailDto, EmailSectionDto } from '../../models';
import { MessageService } from '../../services/message.service';
import { FileDownloaderService } from '../file-downloader/file-downloader.component';
import { DownloadButtonDirective } from '../../directives/download-button.directive';

@Component({
  selector: 'app-message-sections',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    DownloadButtonDirective
  ],
  template: `
    <!-- Sections List -->
    <div class="h-full overflow-auto bg-gray-50 dark:bg-gray-900">
      <div class="p-3 space-y-4">
        <div *ngFor="let section of getMessageSections(); let i = index" class="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
          <!-- Section Header -->
          <div class="p-3">
            <div class="flex items-center gap-3">
              <mat-icon class="text-gray-600 dark:text-gray-400">{{ getSectionIcon(section.fileName || section.mediaType || 'Unknown') }}</mat-icon>
              <div class="flex-1">
                <div class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ section.fileName || section.mediaType || 'Unknown' }}</div>
                <div class="text-gray-600 dark:text-gray-400 text-xs">{{ section.mediaType || 'Unknown type' }}</div>
              </div>
              <div class="flex items-center gap-2">
                <!-- View Button (for text/plain and text/html without filename) -->
                <button 
                  *ngIf="shouldShowViewButton(section)"
                  mat-icon-button 
                  color="accent"
                  (click)="toggleSectionView(section, i)"
                  [title]="isViewingSection(i) ? 'Collapse content' : 'Expand content'">
                  <mat-icon>{{ isViewingSection(i) ? 'expand_less' : 'expand_more' }}</mat-icon>
                </button>
                <!-- Download Button -->
                <button 
                  *ngIf="shouldShowDownloadButton(section)"
                  mat-icon-button 
                  color="primary"
                  [appDownloadButton]="getDownloadButtonId(section, i)"
                  [downloadUrl]="buildSectionUrl(section, i)"
                  [downloadFilename]="section.fileName || 'section-' + (section.id || i)"
                  title="Download">
                  <mat-icon>download</mat-icon>
                </button>
              </div>
            </div>
          </div>
          
          <!-- Expanded Content Area -->
          <div *ngIf="isViewingSection(i)" class="border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
            <div class="p-3">
              <div *ngIf="isSectionLoading(i)" class="flex items-center justify-center py-8">
                <mat-spinner diameter="32"></mat-spinner>
                <span class="ml-3 text-sm text-gray-600 dark:text-gray-400">Loading content...</span>
              </div>
              <div *ngIf="!isSectionLoading(i)" class="bg-white dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700 overflow-hidden">
                <iframe
                  #sectionIframe
                  class="w-full border-none"
                  style="min-height: 200px; max-height: 400px;"
                  [attr.data-section-index]="i"
                  sandbox="allow-same-origin"
                  frameborder="0"
                  scrolling="auto">
                </iframe>
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

    /* Dark mode iframe background */
    :host-context([data-theme="dark"]) iframe {
      background: #1f2937;
    }
  `]
})
export class MessageSectionsComponent implements AfterViewInit {
  @Input() message: DetailDto | null = null;
  @ViewChildren('sectionIframe') iframes!: QueryList<ElementRef<HTMLIFrameElement>>;
  
  // Section viewing state
  viewingSectionIndex: number | null = null;
  sectionContents: Map<number, string> = new Map();
  loadingSections: Set<number> = new Set();
  iframeContents: Map<number, string> = new Map();

  constructor(
    private messageService: MessageService,
    private fileDownloader: FileDownloaderService
  ) {}

  ngAfterViewInit() {
    // Set up iframe content after view is initialized
    setTimeout(() => {
      this.updateIframeContents();
    }, 0);
    
    // Listen for iframe changes
    this.iframes.changes.subscribe(() => {
      setTimeout(() => {
        this.updateIframeContents();
      }, 0);
    });
  }

  private updateIframeContents() {
    this.iframes.forEach((iframeRef) => {
      const iframe = iframeRef.nativeElement;
      const sectionIndex = parseInt(iframe.getAttribute('data-section-index') || '0', 10);
      
      if (this.isViewingSection(sectionIndex) && this.sectionContents.has(sectionIndex)) {
        const content = this.getSectionContentForViewing(sectionIndex);
        this.setIframeContent(iframe, content, sectionIndex);
      }
    });
  }

  private setIframeContent(iframe: HTMLIFrameElement, content: string, sectionIndex: number) {
    try {
      // Use a more reliable method to set iframe content
      const doc = iframe.contentDocument || iframe.contentWindow?.document;
      if (doc) {
        doc.open();
        doc.write(content);
        doc.close();
        
        console.log(`Successfully set iframe content for section ${sectionIndex}`);
      } else {
        // Fallback to srcdoc if document access fails
        iframe.srcdoc = content;
        console.log(`Fallback: Set srcdoc for section ${sectionIndex}`);
      }
    } catch (error) {
      console.error(`Error setting iframe content for section ${sectionIndex}:`, error);
      // Final fallback
      iframe.srcdoc = content;
    }
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
    console.log('toggleSectionView called with:', section, 'index:', index);
    
    if (this.isViewingSection(index)) {
      // Close the section
      this.viewingSectionIndex = null;
      console.log('Closed section view for index:', index);
    } else {
      // Open the section
      this.viewingSectionIndex = index;
      
      if (!this.sectionContents.has(index)) {
        // Load content if not already loaded
        this.loadSectionContent(section, index);
      } else {
        // Content already loaded, update iframe after view change
        setTimeout(() => {
          this.updateIframeContents();
        }, 0);
      }
    }
  }

  private loadSectionContent(section: EmailSectionDto, index: number) {
    if (!this.message?.id) {
      console.log('Cannot load section - missing message ID');
      return;
    }

    console.log('Loading content for section index:', index, 'section ID:', section.id);
    
    this.loadingSections.add(index);
    
    // Use different endpoints based on whether section has an ID
    const observable = section.id 
      ? this.messageService.getSectionContent(this.message.id, section.id)
      : this.messageService.getSectionByIndex(this.message.id, index);
    
    observable.subscribe({
      next: (content) => {
        console.log('Section content loaded for index', index, ':', content.substring(0, 100) + '...');
        this.sectionContents.set(index, content);
        this.loadingSections.delete(index);
        
        // Update iframe content after loading
        setTimeout(() => {
          this.updateIframeContents();
        }, 0);
      },
      error: (error) => {
        console.error('Error loading section content for index', index, ':', error);
        this.sectionContents.set(index, `<html><body><h2>Error loading section content</h2><p>${error.message || error}</p></body></html>`);
        this.loadingSections.delete(index);
        
        // Update iframe content even for errors
        setTimeout(() => {
          this.updateIframeContents();
        }, 0);
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
    
    console.log(`shouldShowViewButton for section ${section.id}: mediaType="${mediaType}", hasFileName=${hasFileName}`);
    
    // If it has a filename, always download
    if (hasFileName) {
      console.log(`Section ${section.id}: has filename, hiding view button`);
      return false;
    }
    
    // If content type is text/plain or text/html, show view option
    const showView = mediaType === 'text/plain' || mediaType === 'text/html';
    console.log(`Section ${section.id}: showView=${showView}`);
    return showView;
  }

  shouldShowDownloadButton(section: EmailSectionDto): boolean {
    const mediaType = (section.mediaType || '').toLowerCase();
    const hasFileName = !!section.fileName;
    
    console.log(`shouldShowDownloadButton for section ${section.id}: mediaType="${mediaType}", hasFileName=${hasFileName}`);
    
    // If it has a filename, always show download
    if (hasFileName) {
      console.log(`Section ${section.id}: has filename, showing download button`);
      return true;
    }
    
    // Don't show download button for text/plain or text/html (they have view button instead)
    const showDownload = !(mediaType === 'text/plain' || mediaType === 'text/html');
    console.log(`Section ${section.id}: showDownload=${showDownload}`);
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
    
    // Debug output to see what's being generated
    console.log('Formatted content for section', index, ':', formattedContent.substring(0, 500) + '...');
    
    return formattedContent;
  }

  getSectionIcon(type: string): string {
    const lowerType = type.toLowerCase();
    
    if (lowerType.includes('image') || lowerType.includes('.jpg') || lowerType.includes('.png') || lowerType.includes('.gif')) {
      return 'image';
    } else if (lowerType.includes('text') || lowerType.includes('.txt')) {
      return 'description';
    } else if (lowerType.includes('pdf')) {
      return 'picture_as_pdf';
    } else if (lowerType.includes('word') || lowerType.includes('document') || lowerType.includes('.doc')) {
      return 'article';
    } else if (lowerType.includes('spreadsheet') || lowerType.includes('excel') || lowerType.includes('.xls')) {
      return 'table_chart';
    } else if (lowerType.includes('zip') || lowerType.includes('archive') || lowerType.includes('.zip')) {
      return 'archive';
    } else {
      return 'attach_file';
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
