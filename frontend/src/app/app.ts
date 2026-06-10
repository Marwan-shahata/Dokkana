import { Component, signal } from '@angular/core';

type NavItem = {
  label: string;
  hasMenu: boolean;
};

type Feature = {
  title: string;
  text: string;
};

type Product = {
  category: string;
  name: string;
  price: number;
  visual: string;
};

type Testimonial = {
  name: string;
  role: string;
  text: string;
  initials: string;
  theme: string;
};

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly navItems = signal<NavItem[]>([
    { label: 'Furniture', hasMenu: true },
    { label: 'Shop', hasMenu: false },
    { label: 'About Us', hasMenu: false },
    { label: 'Contact', hasMenu: false }
  ]);

  protected readonly features = signal<Feature[]>([
    {
      title: 'Luxury facilities',
      text: 'The advantage of hiring a workspace with us is that gives you comfortable service and all-around facilities.'
    },
    {
      title: 'Affordable Price',
      text: 'You can get a workspace of the highest quality at an affordable price and still enjoy the facilities that are oly here.'
    },
    {
      title: 'Many Choices',
      text: 'We provide many unique work space choices so that you can choose the workspace to your liking.'
    }
  ]);

  protected readonly productTabs = signal(['Chair', 'Beds', 'Sofa', 'Lamp']);

  protected readonly products = signal<Product[]>([
    { category: 'Chair', name: 'Sakarias Armchair', price: 392, visual: 'chair-one' },
    { category: 'Chair', name: 'Baltsar Chair', price: 299, visual: 'chair-two' },
    { category: 'Chair', name: 'Anjay Chair', price: 519, visual: 'chair-three' },
    { category: 'Chair', name: 'Nyantuy Chair', price: 921, visual: 'chair-four' }
  ]);

  protected readonly testimonials = signal<Testimonial[]>([
    {
      name: 'Bang Upin',
      role: 'Pedagang Asongan',
      text: '"Terimakasih banyak, kini ruanganku menjadi lebih mewah dan terlihat mahal"',
      initials: 'BU',
      theme: 'review-bright'
    },
    {
      name: 'Ibuk Sukijan',
      role: 'Ibu Rumah Tangga',
      text: '"Makasih Panto, aku sekarang berasa tinggal di apartment karena barang-barang yang terlihat mewah"',
      initials: 'IS',
      theme: 'review-dark'
    },
    {
      name: 'Mpok Ina',
      role: 'Karyawan Swasta',
      text: '"Sangat terjangkau untuk kantong saya yang tidak terlalu banyak"',
      initials: 'MI',
      theme: 'review-warm'
    }
  ]);
}
