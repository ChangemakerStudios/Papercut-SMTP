import { Routes } from '@angular/router';
import { MessageListComponent } from './components/message-list/message-list.component';
import { MessageDetailComponent } from './components/message-detail/message-detail.component';

export const routes: Routes = [
  { path: '', component: MessageListComponent },
  { path: 'message/:id', component: MessageDetailComponent },
  { path: '**', redirectTo: '' }
]; 