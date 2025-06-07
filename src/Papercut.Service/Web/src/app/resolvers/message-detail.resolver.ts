import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

interface EmailAddress {
  name: string;
  address: string;
}

interface Header {
  name: string;
  value: string;
}

interface Section {
  id: string | null;
  mediaType: string;
  fileName: string | null;
}

interface MessageDetail {
  id: string;
  createdAt: string;
  subject: string;
  from: EmailAddress[];
  to: EmailAddress[];
  cc: EmailAddress[];
  bCc: EmailAddress[];
  htmlBody: string;
  textBody: string;
  headers: Header[];
  sections: Section[];
}

@Injectable({
  providedIn: 'root'
})
export class MessageDetailResolver implements Resolve<MessageDetail> {
  constructor(private http: HttpClient) {}

  resolve(route: ActivatedRouteSnapshot): Observable<MessageDetail> {
    return this.http.get<MessageDetail>(`/api/messages/${route.paramMap.get('id')}`);
  }
} 