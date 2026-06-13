import { Component, computed, signal } from '@angular/core';
import { Navbar } from './shared/navbar/navbar';
import { Footer } from './shared/footer/footer';
import { LandingPage } from './pages/landing-page/landing-page';
import { Profile } from './pages/profile/profile';
import { RouterOutlet } from '@angular/router';


@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar, Footer],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  
}
