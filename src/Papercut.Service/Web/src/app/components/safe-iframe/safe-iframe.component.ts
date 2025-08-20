import { Component, Input, AfterViewInit, ViewChild, ElementRef, OnChanges, SimpleChanges, OnDestroy, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-safe-iframe',
  standalone: true,
  imports: [CommonModule],
  template: `
    <iframe
      #safeIframe
      class="w-full border-none"
      [class]="cssClass"
      [style]="cssStyle"
      sandbox="allow-same-origin"
      frameborder="0"
      scrolling="auto">
    </iframe>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
      height: 100%;
    }
    
    iframe {
      width: 100%;
      height: 100%;
    }
  `]
})
export class SafeIframeComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('safeIframe') iframe?: ElementRef<HTMLIFrameElement>;
  
  @Input() content: string = '';
  @Input() cssClass: string = '';
  @Input() cssStyle: string = '';
  @Input() loadingContent: string = '<html><body style="display: flex; align-items: center; justify-content: center; height: 100vh; font-family: system-ui;">Loading...</body></html>';

  private visibilityChangeListener?: () => void;
  private intersectionObserver?: IntersectionObserver;

  constructor(private ngZone: NgZone) {}

  ngAfterViewInit() {
    // Set initial content after view is initialized
    setTimeout(() => {
      this.updateContent();
    }, 0);

    // Set up visibility monitoring to handle tab switches
    this.setupVisibilityMonitoring();
  }

  ngOnDestroy() {
    this.cleanupVisibilityMonitoring();
  }

  ngOnChanges(changes: SimpleChanges) {
    // Update content when input changes
    if (changes['content'] && !changes['content'].firstChange) {
      setTimeout(() => {
        this.updateContent();
      }, 0);
    }
  }

  private updateContent() {
    if (this.iframe && this.iframe.nativeElement) {
      const contentToSet = this.content || this.loadingContent;
      
      // Extra safety check to ensure iframe is still in DOM
      if (this.iframe.nativeElement.isConnected) {
        this.setIframeContent(this.iframe.nativeElement, contentToSet);
      } else {
        // Iframe not connected to DOM, skipping content update
      }
    }
  }

  private setIframeContent(iframe: HTMLIFrameElement, content: string) {
    try {
      // Primary method: Direct document manipulation
      const doc = iframe.contentDocument || iframe.contentWindow?.document;
      if (doc) {
        doc.open();
        doc.write(content);
        doc.close();
        
        // Successfully set iframe content via document.write
      } else {
        // Fallback to srcdoc if document access fails
        iframe.srcdoc = content;
        // Fallback: Set iframe content via srcdoc
      }
    } catch (error) {
      // Error setting iframe content - using fallback
      // Final fallback
      iframe.srcdoc = content;
    }
  }

  private setupVisibilityMonitoring() {
    if (!this.iframe) return;

    // Monitor when the iframe becomes visible using Intersection Observer
    this.intersectionObserver = new IntersectionObserver((entries) => {
      this.ngZone.run(() => {
        entries.forEach(entry => {
          if (entry.isIntersecting && entry.intersectionRatio > 0) {
            console.log('Iframe became visible, refreshing content');
            setTimeout(() => {
              this.updateContent();
            }, 100); // Small delay to ensure DOM is ready
          }
        });
      });
    }, {
      threshold: 0.1, // Trigger when at least 10% is visible
      rootMargin: '10px'
    });

    this.intersectionObserver.observe(this.iframe.nativeElement);

    // Also listen for document visibility changes (tab switches)
    this.visibilityChangeListener = () => {
      if (!document.hidden && this.iframe) {
        console.log('Document became visible, checking iframe content');
        setTimeout(() => {
          this.checkAndRefreshContent();
        }, 200);
      }
    };

    document.addEventListener('visibilitychange', this.visibilityChangeListener);
  }

  private cleanupVisibilityMonitoring() {
    if (this.intersectionObserver) {
      this.intersectionObserver.disconnect();
      this.intersectionObserver = undefined;
    }

    if (this.visibilityChangeListener) {
      document.removeEventListener('visibilitychange', this.visibilityChangeListener);
      this.visibilityChangeListener = undefined;
    }
  }

  private checkAndRefreshContent() {
    if (!this.iframe) return;

    try {
      const doc = this.iframe.nativeElement.contentDocument;
      const isEmpty = !doc || !doc.body || doc.body.innerHTML.trim() === '';
      
      if (isEmpty) {
        console.log('Iframe content is empty, refreshing');
        this.updateContent();
      }
    } catch (error) {
      console.log('Cannot access iframe content, refreshing anyway');
      this.updateContent();
    }
  }

  /**
   * Force refresh the iframe content - useful for theme changes
   */
  public refreshContent() {
    this.updateContent();
  }
}
