import { Component, Input, AfterViewInit, ViewChild, ElementRef, OnChanges, SimpleChanges, OnDestroy, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastNotificationService } from '../../services/toast-notification.service';

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
      sandbox="allow-same-origin allow-popups allow-popups-to-escape-sandbox"
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
  private hasWrittenContent = false;

  constructor(
    private ngZone: NgZone,
    private toastService: ToastNotificationService
  ) {}

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
    // Write as soon as the content arrives -- deferring by a frame here was
    // visible as a flash of the loading placeholder on every message click
    if (changes['content'] && !changes['content'].firstChange) {
      this.updateContent();
    }
  }

  private updateContent() {
    if (!this.iframe?.nativeElement?.isConnected) return;

    // While the next message's content is in flight the host passes empty
    // content. Rewriting the frame with a placeholder then would throw away
    // what is already rendered for no benefit -- the caller covers the frame
    // while it loads -- so only the first write falls back to the placeholder.
    if (!this.content && this.hasWrittenContent) return;

    this.setIframeContent(this.iframe.nativeElement, this.content || this.loadingContent);
  }

  private setIframeContent(iframe: HTMLIFrameElement, content: string) {
    if (this.writeDocument(iframe, content)) {
      return;
    }

    // The frame is unwritable -- it navigated somewhere cross-origin, so its
    // document can no longer be reached. Reset it to a blank same-origin
    // document and write once that load completes, otherwise the message view
    // stays stuck on whatever it navigated to.
    const onLoad = () => {
      iframe.removeEventListener('load', onLoad);

      if (!this.writeDocument(iframe, content)) {
        iframe.srcdoc = content;
      }
    };

    iframe.addEventListener('load', onLoad);
    iframe.src = 'about:blank';
  }

  /** Writes the document directly; returns false when the frame is unreachable. */
  private writeDocument(iframe: HTMLIFrameElement, content: string): boolean {
    try {
      const doc = iframe.contentDocument || iframe.contentWindow?.document;
      if (!doc) return false;

      doc.open();
      doc.write(content);
      doc.close();

      this.attachLinkHandler(doc);
      this.hasWrittenContent = true;
      return true;
    } catch {
      return false;
    }
  }

  /**
   * The message document is written same-origin, so clicks inside it can be
   * handled here rather than by the sandboxed frame. Every link opens in a
   * new tab; file:// links get special treatment because browsers refuse to
   * navigate to them from an http(s) page.
   */
  private attachLinkHandler(doc: Document): void {
    doc.addEventListener('click', (event: MouseEvent) => {
      const target = event.target as HTMLElement | null;
      const anchor = target?.closest?.('a') as HTMLAnchorElement | null;
      if (!anchor) return;

      const href = anchor.getAttribute('href');
      if (!href || href.startsWith('#')) return;

      // The frame can't navigate anywhere useful; handle it from the host
      event.preventDefault();

      this.ngZone.run(() => this.openLink(href));
    });
  }

  private openLink(href: string): void {
    // window.open() would execute a javascript:/data: URL in a window that
    // inherits this origin -- never hand it an email-controlled scheme
    if (/^\s*(javascript|data|vbscript):/i.test(href)) {
      this.toastService.showWarning('Blocked a link with an unsafe scheme');
      return;
    }

    if (/^file:/i.test(href)) {
      // Browsers block file:// navigation from an http(s) page and always
      // will -- hand the path over instead so it can be pasted somewhere useful
      navigator.clipboard?.writeText(href).then(
        () => this.toastService.showInfo('Browsers block file:// links — path copied to the clipboard'),
        () => this.toastService.showWarning(`Browsers block file:// links: ${href}`)
      );
      return;
    }

    window.open(href, '_blank', 'noopener,noreferrer');
  }

  private setupVisibilityMonitoring() {
    if (!this.iframe) return;

    // Monitor when the iframe becomes visible using Intersection Observer
    this.intersectionObserver = new IntersectionObserver((entries) => {
      this.ngZone.run(() => {
        entries.forEach(entry => {
          if (entry.isIntersecting && entry.intersectionRatio > 0) {
            // Becoming visible only needs a rewrite if the frame actually lost
            // its content (tab switch); rewriting unconditionally on a timer
            // delayed every first paint by that timer's length
            this.checkAndRefreshContent();
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
        // document became visible -- check content
        this.checkAndRefreshContent();
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
        // content empty -- refresh
        this.updateContent();
      }
    } catch (error) {
      // content unreachable -- refresh anyway
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
