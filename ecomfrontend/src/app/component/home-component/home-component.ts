import { Component, inject } from '@angular/core';
import { BaseComponent } from '../base.component';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home-component',
  imports: [],
  templateUrl: './home-component.html',
  styleUrl: './home-component.css',
})
export class HomeComponent extends BaseComponent {
  private router = inject(Router)
navigate(tileName: string) {

  switch (tileName) {

    case 'Faculty':
      this.router.navigate(['/faculty']);
      break;

    case 'Module':
      this.router.navigate(['/module']);
      break;

    case 'papers':
      this.router.navigate(['/papers']);
      break;

    case 'scrutiny':
      this.router.navigate(['/scrutiny']);
      break;

    default:
      console.log('No route found');
  }

}
userName: string = "Krishna";
  role: string = "Exam Coordinator";

  tiles = [
    {
      title: "Faculty",
      count: 12
    },
    {
      title: "Module",
      count: 4
    },
    {
      title: "Notifications",
      count: 2
    },
    {
      title: "Duties",
      count: 3
    }
  ];

  logout() {
    console.log("Logout clicked");
  }
}
