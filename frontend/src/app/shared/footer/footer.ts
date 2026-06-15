import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { LanguageService } from '../../core/services/language.service';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './footer.html',
  styleUrl: './footer.css',
})
export class Footer {
  lang = inject(LanguageService);
  readonly year = new Date().getFullYear();

  L(en: string, ar: string): string { return this.lang.t(en, ar); }

  readonly cols = [
    { h: ['Shop', 'تسوّق'], links: [['Dresses', 'فساتين'], ['Shoes', 'أحذية'], ['Bags', 'شنط'], ['Shirts', 'قمصان']] },
    { h: ['Stores', 'المحلات'], links: [['Sell on Dokkana', 'بيع على دكانة'], ['Business Copilot', 'مساعد الأعمال'], ['Store login', 'دخول المحل']] },
    { h: ['Follow Us', 'تابعنا'], links: [['Instagram', 'انستجرام'], ['Facebook', 'فيسبوك'], ['WhatsApp', 'واتساب']] },
  ];
}
