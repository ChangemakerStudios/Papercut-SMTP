import { Pipe, PipeTransform } from '@angular/core';

interface EmailAddress {
  name: string;
  address: string;
}

@Pipe({
  name: 'emailList',
  standalone: true
})
export class EmailListPipe implements PipeTransform {
  transform(emails: EmailAddress[]): string {
    if (!emails?.length) return '';
    return emails.map(email => {
      if (email.name && email.name !== email.address) {
        return `${email.name} <${email.address}>`;
      }
      return email.address;
    }).join(', ');
  }
} 