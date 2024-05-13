import {Component, OnInit} from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import {MessageResponse, MessageService} from "./message.service";
import {HttpClientModule} from "@angular/common/http";
import {AsyncPipe, NgForOf} from "@angular/common";
import {BehaviorSubject, Observable, tap} from "rxjs";

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HttpClientModule, MatSlideToggleModule, NgForOf, AsyncPipe],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'Papercut-SMTP';
  public messages$: BehaviorSubject<MessageResponse> = new BehaviorSubject<MessageResponse>(null);

  constructor(private messageService: MessageService) {
    
  }

  ngOnInit(): void {
    this.messageService.getMessages()
        .pipe(tap(s => {
          this.messages$.next(s);
        }))
        .subscribe();
  }

}
