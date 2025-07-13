import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-resizer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="resizer-handle" 
         (mousedown)="onMouseDown($event)"
         (dblclick)="onDoubleClick()"
         [class.dragging]="isDragging"
         [title]="tooltip">
      <div class="resizer-line"></div>
    </div>
  `,
  styles: [`
    .resizer-handle {
      width: 4px;
      background-color: #e0e0e0;
      cursor: col-resize;
      position: relative;
      transition: background-color 0.2s;
      flex-shrink: 0;
      min-height: 100vh;
    }

    .resizer-handle:hover {
      background-color: #2196f3;
    }

    .resizer-handle.dragging {
      background-color: #2196f3;
    }

    .resizer-handle.dragging::before {
      content: '';
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      cursor: col-resize;
      z-index: 9999;
      user-select: none;
    }

    .resizer-line {
      position: absolute;
      top: 0;
      left: 50%;
      width: 1px;
      height: 100%;
      background-color: rgba(0, 0, 0, 0.1);
      transform: translateX(-50%);
    }

    // Dark mode styles
    :host-context(body[data-theme="dark"]) .resizer-handle {
      background-color: #3e3e42;
    }

    :host-context(body[data-theme="dark"]) .resizer-handle:hover {
      background-color: #4fc3f7;
    }

    :host-context(body[data-theme="dark"]) .resizer-handle.dragging {
      background-color: #4fc3f7;
    }

    :host-context(body[data-theme="dark"]) .resizer-line {
      background-color: rgba(255, 255, 255, 0.15);
    }

    // Responsive design - hide resizer on mobile devices
    @media (max-width: 768px) {
      .resizer-handle {
        display: none;
      }
    }
  `]
})
export class ResizerComponent implements OnInit, OnDestroy {
  @Input() currentWidth = 400;
  @Input() minWidth = 200;
  @Input() maxWidth = 2000;
  @Input() defaultWidth = 400;
  @Input() localStorageKey = 'resizer-width';
  @Input() maxWidthPercentage = 0.8; // 80% of viewport width
  @Input() tooltip = 'Drag to resize, double-click to reset';
  
  @Output() widthChange = new EventEmitter<number>();
  @Output() draggingChange = new EventEmitter<boolean>();

  isDragging = false;

  constructor() {
    // Add window resize listener to adjust width if needed
    window.addEventListener('resize', this.adjustWidthForViewport.bind(this));
  }

  ngOnInit() {
    // Load saved width from localStorage and emit initial width
    this.loadWidth();
    // Defer the width emission to avoid ExpressionChangedAfterItHasBeenCheckedError
    setTimeout(() => {
      this.widthChange.emit(this.currentWidth);
    }, 0);
  }

  ngOnDestroy() {
    window.removeEventListener('resize', this.adjustWidthForViewport.bind(this));
  }

  onMouseDown(event: MouseEvent): void {
    event.preventDefault();
    this.isDragging = true;
    this.draggingChange.emit(true);
    
    const onMouseMove = (moveEvent: MouseEvent) => {
      if (this.isDragging) {
        const newWidth = moveEvent.clientX;
        // Use percentage-based width as primary constraint
        const maxAllowedWidth = window.innerWidth * this.maxWidthPercentage;
        
        if (newWidth >= this.minWidth && newWidth <= maxAllowedWidth) {
          this.currentWidth = newWidth;
          this.widthChange.emit(this.currentWidth);
        }
      }
    };

    const onMouseUp = () => {
      this.isDragging = false;
      this.draggingChange.emit(false);
      this.saveWidth();
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
    };

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
  }

  onDoubleClick(): void {
    this.currentWidth = this.defaultWidth;
    this.widthChange.emit(this.currentWidth);
    this.saveWidth();
  }

  private loadWidth(): void {
    const savedWidth = localStorage.getItem(this.localStorageKey);
    if (savedWidth) {
      const width = parseInt(savedWidth, 10);
      const maxAllowedWidth = window.innerWidth * this.maxWidthPercentage;
      if (width >= this.minWidth && width <= maxAllowedWidth) {
        this.currentWidth = width;
      }
    }
  }

  private saveWidth(): void {
    localStorage.setItem(this.localStorageKey, this.currentWidth.toString());
  }

  private adjustWidthForViewport(): void {
    const maxAllowedWidth = window.innerWidth * this.maxWidthPercentage;
    if (this.currentWidth > maxAllowedWidth) {
      this.currentWidth = maxAllowedWidth;
      this.widthChange.emit(this.currentWidth);
      this.saveWidth();
    }
  }
} 