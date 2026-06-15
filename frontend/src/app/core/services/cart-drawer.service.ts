import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class CartDrawerService {
  readonly openRequests = signal(0);

  open(): void {
    this.openRequests.update(count => count + 1);
  }
}
