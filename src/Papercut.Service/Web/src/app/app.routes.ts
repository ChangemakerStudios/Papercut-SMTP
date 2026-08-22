import { Routes } from '@angular/router';
import { MessageListComponent } from './components/message-list/message-list.component';
import { MessageDetailComponent } from './components/message-detail/message-detail.component';
import { MessageListResolver } from './resolvers/message-list.resolver';

export const routes: Routes = [
  {
    path: '',
    component: MessageListComponent,
    resolve: {
      messages: MessageListResolver
    },
    children: [
      {
        path: 'message/:id',
        component: MessageDetailComponent
      }
    ]
  },
  { path: '**', redirectTo: '' }
]; 