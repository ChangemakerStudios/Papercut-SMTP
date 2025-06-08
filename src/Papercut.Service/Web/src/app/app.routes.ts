import { Routes } from '@angular/router';
import { MessageListComponent } from './components/message-list/message-list.component';
import { MessageListResolver } from './resolvers/message-list.resolver';

export const routes: Routes = [
  {
    path: '',
    component: MessageListComponent,
    resolve: {
      messages: MessageListResolver
    }
  },
  { path: '**', redirectTo: '' }
]; 