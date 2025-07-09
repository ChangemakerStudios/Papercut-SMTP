import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { MessageService } from '../../services/message.service';

@Component({
  selector: 'app-message-details',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatTabsModule, FileSizePipe],
  template: `
    <div class="message-details flex-column flex-fill">
      <div *ngIf="message; else noMessage" class="flex-column flex-fill">
        <!-- Message Header -->
        <div class="message-details-header flex-none">
          <h1 class="message-subject">{{ message.subject || '(No Subject)' }}</h1>
          
          <div class="message-from">
            <mat-icon>person</mat-icon>
            <span>{{ getFromDisplay(message) }}</span>
          </div>
          
          <div class="message-meta">
            <div class="message-meta-item">
              <mat-icon>schedule</mat-icon>
              <span>{{ message.date | date:'MMM d, y h:mm a' }}</span>
            </div>
            <div class="message-meta-item">
              <mat-icon>storage</mat-icon>
              <span>{{ message.size | fileSize }}</span>
            </div>
            <div class="message-meta-item" *ngIf="message.to?.length">
              <mat-icon>email</mat-icon>
              <span>To: {{ getToDisplay(message) }}</span>
            </div>
          </div>
        </div>

        <!-- Tabbed Content -->
        <div class="message-tabs-container flex-column flex-fill">
          <mat-tab-group class="message-tabs flex-column flex-fill" animationDuration="0ms">
            <mat-tab label="Message">
              <div class="tab-content flex-column flex-fill">
                <iframe
                  class="message-iframe flex-fill"
                  [srcdoc]="messageService.getMessageContent(message)"
                  sandbox="allow-same-origin"
                  frameborder="0"
                ></iframe>
              </div>
            </mat-tab>
            
            <mat-tab label="Headers">
              <div class="tab-content flex-column flex-fill">
                <div class="headers-content flex-fill">
                  <div class="header-item" *ngFor="let header of getHeaders(message)">
                    <span class="header-name">{{ header.name }}:</span>
                    <span class="header-value">{{ header.value }}</span>
                  </div>
                </div>
              </div>
            </mat-tab>
            
            <mat-tab label="Body">
              <div class="tab-content flex-column flex-fill">
                <div class="body-content flex-fill">
                  <pre>{{ getBodyContent(message) }}</pre>
                </div>
              </div>
            </mat-tab>
            
            <mat-tab label="Sections">
              <div class="tab-content flex-column flex-fill">
                <div class="sections-content flex-fill">
                  <div class="section-item" *ngFor="let section of getSections(message)">
                    <div class="section-header">
                      <mat-icon>{{ getSectionIcon(section.type) }}</mat-icon>
                      <span class="section-type">{{ section.type }}</span>
                      <span class="section-info">{{ section.info }}</span>
                    </div>
                    <div class="section-content" *ngIf="section.content">
                      <pre>{{ section.content }}</pre>
                    </div>
                  </div>
                </div>
              </div>
            </mat-tab>
            
            <mat-tab label="Raw">
              <div class="tab-content flex-column flex-fill">
                <div class="raw-content flex-fill">
                  <pre>{{ getRawContent(message) }}</pre>
                </div>
              </div>
            </mat-tab>
          </mat-tab-group>
        </div>
      </div>

      <!-- No Message Selected State -->
      <ng-template #noMessage>
        <div class="no-message flex-column flex-fill flex-center">
          <mat-icon>inbox</mat-icon>
          <h3>No Message Selected</h3>
          <p>Choose a message from the list to view its contents and details here.</p>
        </div>
      </ng-template>
    </div>
  `
})
export class MessageDetailsComponent {
  @Input() message: any;
  constructor(public messageService: MessageService) {}

  getFromDisplay(message: any): string {
    if (!message?.from?.length) return 'Unknown Sender';
    const sender = message.from[0];
    return sender.name && sender.name !== sender.address 
      ? `${sender.name} <${sender.address}>`
      : sender.address;
  }

  getToDisplay(message: any): string {
    if (!message?.to?.length) return '';
    if (message.to.length === 1) {
      const recipient = message.to[0];
      return recipient.name && recipient.name !== recipient.address 
        ? recipient.name
        : recipient.address;
    }
    return `${message.to.length} recipients`;
  }

  getHeaders(message: any): { name: string; value: string }[] {
    if (!message) return [];
    
    const headers: { name: string; value: string }[] = [];
    
    // Add standard headers
    if (message.from?.length) {
      headers.push({ name: 'From', value: this.getFromDisplay(message) });
    }
    if (message.to?.length) {
      headers.push({ name: 'To', value: message.to.map((t: any) => 
        t.name && t.name !== t.address ? `${t.name} <${t.address}>` : t.address
      ).join(', ') });
    }
    if (message.cc?.length) {
      headers.push({ name: 'CC', value: message.cc.map((c: any) => 
        c.name && c.name !== c.address ? `${c.name} <${c.address}>` : c.address
      ).join(', ') });
    }
    if (message.bcc?.length) {
      headers.push({ name: 'BCC', value: message.bcc.map((b: any) => 
        b.name && b.name !== b.address ? `${b.name} <${b.address}>` : b.address
      ).join(', ') });
    }
    if (message.subject) {
      headers.push({ name: 'Subject', value: message.subject });
    }
    if (message.date) {
      headers.push({ name: 'Date', value: new Date(message.date).toString() });
    }
    if (message.messageId) {
      headers.push({ name: 'Message-ID', value: message.messageId });
    }
    
    // Add any additional headers from the message object
    if (message.headers) {
      Object.keys(message.headers).forEach(key => {
        if (!headers.find(h => h.name.toLowerCase() === key.toLowerCase())) {
          headers.push({ name: key, value: message.headers[key] });
        }
      });
    }
    
    return headers;
  }

  getBodyContent(message: any): string {
    if (!message) return '';
    
    // Try to get plain text body first
    if (message.textBody) {
      return message.textBody;
    }
    
    // Fall back to HTML body without tags
    if (message.htmlBody) {
      return message.htmlBody.replace(/<[^>]*>/g, '');
    }
    
    // Fall back to any body content
    if (message.body) {
      return message.body;
    }
    
    return 'No body content available';
  }

  getSections(message: any): { type: string; info: string; content?: string }[] {
    if (!message) return [];
    
    const sections: { type: string; info: string; content?: string }[] = [];
    
    // Add HTML section if available
    if (message.htmlBody) {
      sections.push({
        type: 'HTML Body',
        info: `${message.htmlBody.length} characters`,
        content: message.htmlBody
      });
    }
    
    // Add text section if available
    if (message.textBody) {
      sections.push({
        type: 'Text Body',
        info: `${message.textBody.length} characters`,
        content: message.textBody
      });
    }
    
    // Add attachments section if available
    if (message.attachments?.length) {
      sections.push({
        type: 'Attachments',
        info: `${message.attachments.length} file(s)`,
        content: message.attachments.map((att: any) => 
          `${att.name || 'Unnamed'} (${att.contentType || 'unknown type'}, ${att.size || 0} bytes)`
        ).join('\n')
      });
    }
    
    return sections;
  }

  getSectionIcon(type: string): string {
    switch (type.toLowerCase()) {
      case 'html body':
        return 'code';
      case 'text body':
        return 'text_fields';
      case 'attachments':
        return 'attachment';
      default:
        return 'description';
    }
  }

  getRawContent(message: any): string {
    if (!message) return '';
    
    // Try to get the raw message content
    if (message.raw) {
      return message.raw;
    }
    
    // Fall back to reconstructing from available data
    let raw = '';
    
    // Add headers
    this.getHeaders(message).forEach(header => {
      raw += `${header.name}: ${header.value}\n`;
    });
    
    raw += '\n';
    
    // Add body content
    if (message.htmlBody) {
      raw += message.htmlBody;
    } else if (message.textBody) {
      raw += message.textBody;
    } else if (message.body) {
      raw += message.body;
    }
    
    return raw || 'Raw content not available';
  }
} 