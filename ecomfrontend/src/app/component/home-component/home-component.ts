
import { Component, inject } from '@angular/core';
import { BaseComponent } from '../base.component';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

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
  imports: [CommonModule, FormsModule],
  templateUrl: './home-component.html',
  styleUrl: './home-component.css',
})
export class HomeComponent extends BaseComponent {

  private router = inject(Router);

  userName: string = "Dr.John";
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

  // separate message for each module
  moduleCoordinatorMessage: { [key: number]: string } = {};

// ================= BUTTON COLOR STATE =================

examConfirmed: boolean = false;
confirmedModules: { [key: number]: boolean } = {};
  // ================= ASSIGN FUNCTIONS =================

  confirmExamCoordinator() {

  if (this.selectedExamCoordinator) {

    this.examCoordinator = this.selectedExamCoordinator;

    this.examCoordinatorMessage =
      "Exam Coordinator assigned successfully!";

    this.examConfirmed = true;

    this.moduleCoordinatorMessage = {};

    setTimeout(() => {
      this.examCoordinatorMessage = "";
    }, 3000);
  }

}


  confirmModuleCoordinator(module: Module) {

  const selected = this.selectedModuleCoordinator[module.id];

  if (selected) {

    module.coordinator = selected;

    this.moduleCoordinatorMessage[module.id] =
      module.name + " coordinator assigned successfully!";

    // mark module as confirmed
    this.confirmedModules[module.id] = true;

    this.examCoordinatorMessage = "";

    setTimeout(() => {
      this.moduleCoordinatorMessage[module.id] = "";
    }, 3000);
  }

}



  // ================= CREATE MODULE =================

  newModule = {
    code: '',
    name: '',
    subjects: [] as Subject[]
  };

  subjectCode: string = '';
  subjectName: string = '';

  addSubject() {

    if (this.subjectCode && this.subjectName) {

      this.newModule.subjects.push({
        code: this.subjectCode,
        name: this.subjectName
      });

      this.subjectCode = '';
      this.subjectName = '';
    }

  }

  removeSubject(subject: Subject) {

    this.newModule.subjects =
      this.newModule.subjects.filter(s => s !== subject);

  }

  createModule() {

    if (!this.newModule.name) {
      alert("Please enter module name");
      return;
    }

    const newId = this.moduleDetails.length + 1;

    const module: Module = {
      id: newId,
      name: this.newModule.name,
      coordinator: "Not assigned",
      subjects: [...this.newModule.subjects],
      teachers: []
    };

    this.moduleDetails.push(module);

    this.newModule = {
      code: '',
      name: '',
      subjects: []
    };

    this.subjectCode = '';
    this.subjectName = '';

    console.log("Module created:", module);

  }



  // ================= EDIT MODULE =================

  editModule: Module = {
    id: 0,
    name: '',
    coordinator: '',
    subjects: [],
    teachers: []
  };

  openEditModule(module: Module) {
    this.editModule = JSON.parse(JSON.stringify(module));
  }

  addSubjectToEdit() {

    if (this.subjectCode && this.subjectName) {

      this.editModule.subjects.push({
        code: this.subjectCode,
        name: this.subjectName
      });

      this.subjectCode = '';
      this.subjectName = '';
    }

  }

  removeSubjectFromEdit(subject: Subject) {

    this.editModule.subjects =
      this.editModule.subjects.filter(s => s !== subject);

  }

  updateModule() {

    const index = this.moduleDetails.findIndex(
      m => m.id === this.editModule.id
    );

    if (index !== -1) {
      this.moduleDetails[index] = this.editModule;
    }

  }
  

}
