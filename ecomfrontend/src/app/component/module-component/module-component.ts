import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';



// ================= INTERFACES =================
interface Paper {
  module: string;
  subject: string;
  faculty: string;
  uploaded: boolean;
  scrutinizerAssigned: boolean;
}

interface ModuleDetails {
  id: number;
  name: string;
  code: string;
  coordinator: string;
  subjects: { code: string; name: string }[];
  teachers: { id: string; name: string }[];
}

@Component({
  selector: 'app-module-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './module-component.html',
  styleUrls: ['./module-component.css'],
})
export class ModuleComponent {
  

  private router = inject(Router);

  // ================= USER =================
  userName = "Prof. Sunil KS";
  role = "Exam Coordinator";

  // ================= SIDEBAR =================
  selectedMenu = "Dashboard";

  tiles = [
    { title: "Dashboard" },
    { title: "My Module" },
    { title: "Assign Scrutinizer" },
   
    
    { title: "QN Paper" },
    { title: "Scrutiny Report" }
  ];

  navigate(menu: string) {
    this.selectedMenu = menu;
  }

  // ================= MODULE TABLE =================
  columns = [
    { field: 'ModuleId', header: 'ID' },
    { field: 'ModuleName', header: 'Subject' },
    { field: 'Noofsubjects', header: 'No of subjects' },
  ];

  // ================= MODULE DETAILS (RIGHT PANEL) =================
  module: ModuleDetails = {
    id: 1,
    name: 'Data Structures and Algorithms',
    code: 'CS101',
    coordinator: 'Prof. Sunil KS',
    subjects: [
      { code: 'CS101A', name: 'Arrays and Linked Lists' },
      { code: 'CS101B', name: 'Trees and Graphs' },
      { code: 'CS101C', name: 'Sorting Algorithms' },
      { code: 'CS101D', name: 'Dynamic Programming' }
    ],
    teachers: [
      { id: 'T01', name: 'Dr. A' },
      { id: 'T02', name: 'Dr. B' },
      { id: 'T03', name: 'Dr. C' }
    ]
  };

  // ================= PAPERS DATA =================
  papers: Paper[] = [
    {
      module: 'Module 1',
      subject: 'CS301 - Algorithms',
      faculty: 'Dr. A',
      uploaded: true,
      scrutinizerAssigned: false
    },
    {
      module: 'Module 1',
      subject: 'CS302 - Networks',
      faculty: 'Dr. B',
      uploaded: true,
      scrutinizerAssigned: false
    },
    {
      module: 'Module 2',
      subject: 'CS303 - DBMS',
      faculty: 'Dr. C',
      uploaded: true,
      scrutinizerAssigned: true
    }
  ];

  // ================= DASHBOARD =================
 get pendingAssignCount(): number {
  return this.Papers.filter(p =>
    p.status === 'Submitted' && !p.scrutinizerAssigned
  ).length;
}
  goToAssign() {
    this.selectedMenu = 'Assign Scrutinizer';
  }

  // ================= ASSIGN =================
  modules: string[] = ['Module 1', 'Module 2'];

  subjects = [
    { code: 'CS301', name: 'Algorithms', module: 'Module 1' },
    { code: 'CS302', name: 'Networks', module: 'Module 1' },
    { code: 'CS303', name: 'DBMS', module: 'Module 2' }
  ];

  faculties: string[] = ['Dr. X', 'Dr. Y', 'Dr. Z'];

  selectedModule: string = '';
  selectedSubject: string = '';
  selectedFaculty: string = '';

  assignments: any[] = [];
  assignSuccess: boolean = false;

  assignFaculty() {
    if (!this.selectedModule || !this.selectedSubject || !this.selectedFaculty) {
      alert('Please select all fields');
      return;
    }

    // Save assignment
    this.assignments.push({
      module: this.selectedModule,
      subject: this.selectedSubject,
      faculty: this.selectedFaculty
    });

    // Update paper status
    this.papers.forEach(p => {
      if (this.selectedSubject.includes(p.subject.split(' - ')[0])) {
        p.scrutinizerAssigned = true;
      }
    });

    this.assignSuccess = true;

    // Reset form
    this.selectedModule = '';
    this.selectedSubject = '';
    this.selectedFaculty = '';

    setTimeout(() => {
      this.assignSuccess = false;
    }, 2000);
  }

  // ================= OPTIONAL: DYNAMIC MODULE CHANGE =================
  selectModule(moduleName: string) {
    this.selectedModule = moduleName;

    if (moduleName === 'Module 1') {
      this.module = {
        id: 1,
        name: 'Module 1',
        code: 'CS101',
        coordinator: 'Prof. Sunil KS',
        subjects: this.subjects.filter(s => s.module === 'Module 1'),
        teachers: [
          { id: 'T01', name: 'Dr. A' },
          { id: 'T02', name: 'Dr. B' }
        ]
      };
    }

    if (moduleName === 'Module 2') {
      this.module = {
        id: 2,
        name: 'Module 2',
        code: 'CS102',
        coordinator: 'Dr. Ajay James',
        subjects: this.subjects.filter(s => s.module === 'Module 2'),
        teachers: [
          { id: 'T03', name: 'Dr. C' }
        ]
      };
    }
  }

  // ================= NOTIFICATIONS =================
  notifications: string[] = [
    '2 papers pending assignment'
  ];
  showNotifications = false;

  toggleNotifications() {
    this.showNotifications = !this.showNotifications;
  }

  // ================= LOGOUT =================
 logout() {
  // clear session / stored data
  localStorage.clear();

  // navigate to login page
  this.router.navigate(['/login']);
}

  // ================= QN PAPERS =================
 Papers = [
  {
    id: 'QP001',
    subject: 'Data Structures',
    faculty: 'Prof. Sunil KS',
    submittedDate: '10/02/2026',
    scrutinizer: 'Dr. Ajay James',
    status: 'Approved',
    uploaded: true,
    scrutinizerAssigned: true
  },
  {
    id: 'QP002',
    subject: 'Operating Systems',
    faculty: 'Prof. Philumon Joseph',
    submittedDate: '12/02/2026',
    scrutinizer: '',
    status: 'Submitted',
    uploaded: true,
    scrutinizerAssigned: false
  },
  {
    id: 'QP003',
    subject: 'Database Management Systems',
    faculty: 'Prof. Sangeeth',
    submittedDate: '13/02/2026',
    scrutinizer: 'Dr. Sangeeth',
    status: 'Under Scrutiny',
    uploaded: true,
    scrutinizerAssigned: true
  },
  {
    id: 'QP004',
    subject: 'Computer Networks',
    faculty: 'Dr. Priya Sharma',
    submittedDate: '14/02/2026',
    scrutinizer: '',
    status: 'Submitted',
    uploaded: true,
    scrutinizerAssigned: false
  },
  {
    id: 'QP005',
    subject: 'Software Engineering',
    faculty: 'Dr. Rajesh Kumar',
    submittedDate: '',
    scrutinizer: '',
    status: 'Not Submitted',
    uploaded: false,
    scrutinizerAssigned: false
  }
];

  facultiesList: string[] = [
  'Dr. Ajay James',
  'Dr. Sangeeth',
  'Dr. Priya Sharma',
  'Dr. Rajesh Kumar'
];

selectedFilterFaculty: string = 'All Faculties';
assignScrutinizer(paper: any, faculty: string) {

  // 🚫 Block if not submitted
  if (paper.status === 'Not Submitted') {
    alert("Cannot assign. Paper not submitted.");
    return;
  }

  if (!faculty) return;

  paper.scrutinizer = faculty;
  paper.scrutinizerAssigned = true;
  paper.status = 'Under Scrutiny';
}
editingPaperId: string | null = null;
startEdit(paper: any) {
  this.editingPaperId = paper.id;
}

saveEdit(paper: any) {
  if (!paper.scrutinizer) return;

  paper.scrutinizerAssigned = true;
  paper.status = 'Under Scrutiny';

  this.editingPaperId = null;
}

cancelEdit() {
  this.editingPaperId = null;
}

 /* ================= QN PAPER ================= */

  uploadedFile: File | null = null;

  uploadQnPaper(event: any) {
    this.uploadedFile = event.target.files[0];
  }

  submitQnPaper() {

    if (!this.uploadedFile) {
      alert("Please select a file first");
      return;
    }

    alert("Question Paper uploaded successfully!");
    this.uploadedFile = null;
  }

  /* ================= SCRUTINY ================= */

  showScrutinyPopup = false;
  selectedPaper: any;

  scrutinyQuestions = [
    { question: "Question 1", remark: "" },
    { question: "Question 2", remark: "" },
    { question: "Question 3", remark: "" }
  ];

  openScrutinyPopup(paper: any) {
    this.selectedPaper = paper;
    this.showScrutinyPopup = true;
  }

  closeScrutinyPopup() {
    this.showScrutinyPopup = false;
  }

  submitScrutinyPopup() {
    alert("Scrutiny Report Submitted Successfully");
    this.showScrutinyPopup = false;
  }

 viewPdf(paper: any) {

  if (!paper.file) {
    alert("No PDF uploaded");
    return;
  }

  const fileURL = URL.createObjectURL(paper.file);
  window.open(fileURL, '_blank');
}

}