import { Routes } from '@angular/router';
import { MessageListComponent } from './components/message-list/message-list.component';
import { MessageDetailComponent } from './components/message-detail/message-detail.component';
import { MessageListResolver } from './resolvers/message-list.resolver';
import { MessageDetailResolver } from './resolvers/message-detail.resolver';

export const routes: Routes = [
  {
    path: '',
    component: MessageListComponent,
    resolve: {
      messages: MessageListResolver
    }
  },
  {
    path: 'message/:id',
    component: MessageDetailComponent,
    resolve: {
      message: MessageDetailResolver
    }
  },
  { path: '**', redirectTo: '' }
]; 