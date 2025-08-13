import { Directive, Input, ElementRef, OnInit, OnDestroy, Renderer2 } from '@angular/core';
import { Subscription } from 'rxjs';
import { FileDownloaderService } from '../components/file-downloader/file-downloader.component';

@Directive({
  selector: '[appDownloadButton]',
  standalone: true
})
export class DownloadButtonDirective implements OnInit, OnDestroy {
  @Input() appDownloadButton!: string; // Button ID
  @Input() downloadUrl!: string;
  @Input() downloadFilename?: string;

  private subscription?: Subscription;
  private originalContent?: string;
  private isLoading = false;

  constructor(
    private el: ElementRef,
    private renderer: Renderer2,
    private fileDownloader: FileDownloaderService
  ) {}

  ngOnInit() {
    // Store original button content
    this.originalContent = this.el.nativeElement.innerHTML;

    // Subscribe to loading state
    this.subscription = this.fileDownloader.isButtonDownloading(this.appDownloadButton)
      .subscribe(isLoading => {
        this.isLoading = isLoading;
        this.updateButtonState(isLoading);
      });

    // Add click handler
    this.renderer.listen(this.el.nativeElement, 'click', (event) => {
      if (!this.isLoading) {
        event.preventDefault();
        this.startDownload();
      }
    });
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  private startDownload() {
    this.fileDownloader.downloadFile(
      this.downloadUrl,
      this.downloadFilename,
      this.appDownloadButton
    );
  }

  private updateButtonState(isLoading: boolean) {
    if (isLoading) {
      // Disable button and show spinner
      this.renderer.setAttribute(this.el.nativeElement, 'disabled', 'true');
      this.renderer.addClass(this.el.nativeElement, 'downloading');
      this.renderer.setStyle(this.el.nativeElement, 'opacity', '0.7');
      this.renderer.setStyle(this.el.nativeElement, 'pointer-events', 'none');
      
      // Find mat-icon inside button and hide it
      const matIcon = this.el.nativeElement.querySelector('mat-icon');
      if (matIcon) {
        this.renderer.setStyle(matIcon, 'display', 'none');
      }
      
      // Create a simple spinner using CSS
      const spinner = this.renderer.createElement('div');
      this.renderer.addClass(spinner, 'download-spinner');
      this.renderer.setStyle(spinner, 'width', '16px');
      this.renderer.setStyle(spinner, 'height', '16px');
      this.renderer.setStyle(spinner, 'border', '2px solid #f3f3f3');
      this.renderer.setStyle(spinner, 'border-top', '2px solid #3498db');
      this.renderer.setStyle(spinner, 'border-radius', '50%');
      this.renderer.setStyle(spinner, 'animation', 'spin 1s linear infinite');
      this.renderer.setStyle(spinner, 'display', 'inline-block');
      this.renderer.appendChild(this.el.nativeElement, spinner);
      
      // Add keyframe animation if not exists
      this.addSpinAnimation();
    } else {
      // Re-enable button and restore original content
      this.renderer.removeAttribute(this.el.nativeElement, 'disabled');
      this.renderer.removeClass(this.el.nativeElement, 'downloading');
      this.renderer.removeStyle(this.el.nativeElement, 'opacity');
      this.renderer.removeStyle(this.el.nativeElement, 'pointer-events');
      
      // Remove spinner
      const spinner = this.el.nativeElement.querySelector('.download-spinner');
      if (spinner) {
        this.renderer.removeChild(this.el.nativeElement, spinner);
      }
      
      // Show mat-icon again
      const matIcon = this.el.nativeElement.querySelector('mat-icon');
      if (matIcon) {
        this.renderer.removeStyle(matIcon, 'display');
      }
    }
  }

  private addSpinAnimation() {
    const styleId = 'download-spinner-keyframes';
    if (!document.getElementById(styleId)) {
      const style = this.renderer.createElement('style');
      this.renderer.setAttribute(style, 'id', styleId);
      this.renderer.setProperty(style, 'textContent', `
        @keyframes spin {
          0% { transform: rotate(0deg); }
          100% { transform: rotate(360deg); }
        }
      `);
      this.renderer.appendChild(document.head, style);
    }
  }
}
