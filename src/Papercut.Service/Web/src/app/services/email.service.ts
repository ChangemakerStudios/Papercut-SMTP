import { Injectable } from '@angular/core';
import { EmailAddressDto } from '../models/email-address-dto';

@Injectable({
  providedIn: 'root'
})
export class EmailService {

  /**
   * Formats an email address in the standard format: "Name <address@example.com>"
   * If no name is provided, returns just the address.
   * 
   * @param emailAddress The email address DTO to format
   * @returns Formatted email string
   */
  formatEmailAddress(emailAddress: EmailAddressDto): string {
    if (!emailAddress?.address) {
      return 'Unknown Sender';
    }

    if (emailAddress.name && emailAddress.name !== emailAddress.address) {
      return `${emailAddress.name} <${emailAddress.address}>`;
    }

    return emailAddress.address;
  }

  /**
   * Formats multiple email addresses as a comma-separated string
   * 
   * @param emailAddresses Array of email address DTOs
   * @returns Formatted email list string
   */
  formatEmailAddressList(emailAddresses: EmailAddressDto[]): string {
    if (!emailAddresses?.length) {
      return '';
    }

    return emailAddresses
      .map(addr => this.formatEmailAddress(addr))
      .join(', ');
  }

  /**
   * Gets the display name for an email address (name if available, otherwise address)
   * 
   * @param emailAddress The email address DTO
   * @returns Display name or address
   */
  getDisplayName(emailAddress: EmailAddressDto): string {
    if (!emailAddress?.address) {
      return 'Unknown Sender';
    }

    if (emailAddress.name && emailAddress.name !== emailAddress.address) {
      return emailAddress.name;
    }

    return emailAddress.address;
  }

  /**
   * Gets the display name for the first email address in a list
   * 
   * @param emailAddresses Array of email address DTOs
   * @returns Display name for the first address
   */
  getFirstDisplayName(emailAddresses: EmailAddressDto[]): string {
    if (!emailAddresses?.length) {
      return 'Unknown Sender';
    }

    return this.getDisplayName(emailAddresses[0]);
  }

  /**
   * Extracts just the email address portion from an EmailAddressDto
   * 
   * @param emailAddress The email address DTO
   * @returns Just the email address string
   */
  getEmailAddress(emailAddress: EmailAddressDto): string {
    return emailAddress?.address || '';
  }

  /**
   * Checks if an email address has a display name
   * 
   * @param emailAddress The email address DTO
   * @returns True if the address has a name different from the address
   */
  hasDisplayName(emailAddress: EmailAddressDto): boolean {
    return !!(emailAddress?.name && emailAddress.name !== emailAddress.address);
  }
} 