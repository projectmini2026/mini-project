
import { Component, inject } from '@angular/core';
import { BaseComponent } from '../base.component';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

interface Subject {
  code: string;
  name: string;
}

interface Module {
  id: number;
  name: string;
  coordinator: string;
  subjects: Subject[];
  teachers: string[];
}

@Component({
  selector: 'app-home-component',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './home-component.html',
  styleUrl: './home-component.css',
})
export class HomeComponent extends BaseComponent {

  private router = inject(Router);

  userName: string = "Krishna";
  role: string = "HOD";

  selectedMenu: string = "Dashboard";

  tiles = [
    { title: "Dashboard" },
    { title: "Module Management" },
    { title: "Faculty Management" }
  ];

  navigate(tileName: string) {
    this.selectedMenu = tileName;
  }

  logout() {
    console.log("Logout clicked");
  }


  // ================= DASHBOARD =================

  examCoordinator: string = "Dr. Anil Kumar";

  modules = [
    { name: "Data Structures" },
    { name: "Operating Systems" },
    { name: "Computer Networks" },
    { name: "Software Engineering" }
  ];


  // ================= MODULE DATA =================

  moduleDetails: Module[] = [

    {
      id: 1,
      name: "Data Structures",
      coordinator: "Dr. Midhun Mathew",
      subjects: [
        { code: "CS201", name: "Data Structures" },
        { code: "CS202", name: "Algorithms" }
      ],
      teachers: [
        "Prof. Amalu Michael",
        "Dr. Rakesh Kumar"
      ]
    },

    {
      id: 2,
      name: "Database Systems",
      coordinator: "Dr. Meera Nair",
      subjects: [
        { code: "CS301", name: "DBMS" },
        { code: "CS302", name: "Advanced Databases" }
      ],
      teachers: [
        "Prof. Neha Nair",
        "Dr. Anil Kumar"
      ]
    },

    {
      id: 3,
      name: "Operating Systems",
      coordinator: "Dr. Joseph Thomas",
      subjects: [
        { code: "CS401", name: "Operating Systems" }
      ],
      teachers: [
        "Dr. Rakesh Kumar"
      ]
    },

    {
      id: 4,
      name: "Computer Networks",
      coordinator: "Dr. Meera Nair",
      subjects: [
        { code: "CS501", name: "Computer Networks" }
      ],
      teachers: [
        "Prof. Amalu Michael"
      ]
    }

  ];


  // ================= FACULTY LIST =================

  allTeachers: string[] = [
    "Prof. Amalu Michael",
    "Dr. Rakesh Kumar",
    "Prof. Neha Nair",
    "Dr. Anil Kumar",
    "Dr. Joseph Thomas",
    "Dr. Meera Nair"
  ];


  // ================= TEMP SELECTION =================

  selectedExamCoordinator: string = this.examCoordinator;

  selectedModuleCoordinator: { [key: number]: string } = {};


  // ================= CONFIRMATION MESSAGES =================

  examCoordinatorMessage: string = "";
  moduleCoordinatorMessage: string = "";


  // ================= ASSIGN FUNCTIONS =================

  confirmExamCoordinator() {

    if (this.selectedExamCoordinator) {

      this.examCoordinator = this.selectedExamCoordinator;

      this.examCoordinatorMessage =
        "Exam Coordinator assigned successfully!";

      this.moduleCoordinatorMessage = "";
    }
  }


  confirmModuleCoordinator(module: Module) {

    const selected = this.selectedModuleCoordinator[module.id];

    if (selected) {

      module.coordinator = selected;

      this.moduleCoordinatorMessage =
        module.name + " coordinator assigned successfully!";

      this.examCoordinatorMessage = "";
    }
  }

}

