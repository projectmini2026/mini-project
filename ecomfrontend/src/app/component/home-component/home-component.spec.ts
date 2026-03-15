import { Component } from '@angular/core';

interface Module {
  id: number;
  name: string;
  coordinator: string;
  subjects: { code: string; name: string }[];
  teachers: string[];
}

@Component({
  selector: 'app-home',
  templateUrl: './home-component.html',
  styleUrls: ['./home-component.css']
})
export class HomeComponent {
  // User info
  userName: string = 'Dr. Krishna';
  role: string = 'HOD';

  // Sidebar
  selectedMenu: string = 'Dashboard';
  tiles = [
    { title: 'Dashboard' },
    { title: 'Module Management' },
    { title: 'Faculty Management' }
  ];

  // Exam Coordinator
  examCoordinator: string = '';
  selectedExamCoordinator: string = '';
  examCoordinatorMessage: string = '';

  // Module Coordinators
  selectedModuleCoordinator: { [key: number]: string } = {};
  moduleCoordinatorMessage: string = '';

  // Teachers
  allTeachers: string[] = ['John', 'Mary', 'Alex', 'Emma', 'David'];

  // Modules
  moduleDetails: Module[] = [
    {
      id: 1,
      name: 'Module 1',
      coordinator: '',
      subjects: [
        { code: 'CS101', name: 'Data Structures' },
        { code: 'CS102', name: 'Algorithms' }
      ],
      teachers: ['John', 'Alex']
    },
    {
      id: 2,
      name: 'Module 2',
      coordinator: '',
      subjects: [
        { code: 'CS201', name: 'Operating Systems' },
        { code: 'CS202', name: 'Computer Networks' }
      ],
      teachers: ['Mary', 'David']
    }
  ];

  // Dashboard modules list
  modules = this.moduleDetails;

  // Navigation
  navigate(menu: string) {
    this.selectedMenu = menu;
  }

  // Logout
  logout() {
    console.log('Logged out!');
  }

  // Assign Exam Coordinator
  confirmExamCoordinator() {
    if (this.selectedExamCoordinator) {
      this.examCoordinator = this.selectedExamCoordinator;
      this.examCoordinatorMessage = `Exam Coordinator assigned successfully!`;
    }
  }

  // Assign Module Coordinator
  confirmModuleCoordinator(module: Module) {
    const selected = this.selectedModuleCoordinator[module.id];
    if (selected) {
      module.coordinator = selected;
      this.moduleCoordinatorMessage = `Module Coordinator assigned successfully!`;
    }
  }
}