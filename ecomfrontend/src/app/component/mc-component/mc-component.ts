 import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

interface Subject {
  code: string;
  name: string;
  module: string;
}

interface Module {
  id: number;
  name: string;
  coordinator: string;
  subjects: Subject[];
  teachers: string[];
}

@Component({
  selector: 'app-mc-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './mc-component.html',
  styleUrls: ['./mc-component.css']
})
export class McComponent {

  private router = inject(Router);

  userName = "Prof. Sunil KS";
  role = "Exam Coordinator";

  selectedMenu = "Dashboard";

  tiles = [
    { title: "Dashboard" },
    { title: "Module Management" },
    { title: "Work Flow Status" },
    { title: "Assign" },
    { title: "Download" },
    { title: "QN Paper" },
    { title: "Scrutiny Report" }
  ];

  navigate(menu: string) {
    this.selectedMenu = menu;
  }

  logout() {
    console.log("Logout clicked");
  }

  /* ================= MODULE MANAGEMENT ================= */

  moduleDetails: Module[] = [
    {
      id: 1,
      name: "Data Structures",
      coordinator: "Dr. Midhun Mathew",
      subjects: [
        { code: "CS201", name: "Data Structures", module: "Data Structures" },
        { code: "CS202", name: "Algorithms", module: "Data Structures" }
      ],
      teachers: [
        "Prof. Amalu Michael",
        "Dr. Rakesh Kumar"
      ]
    }
  ];

  /* ================= WORKFLOW STATUS ================= */

  workflowData = [
    {
      module: "Data Structures and Algorithms",
      faculty: "Prof. Sunil KS",
      examDate: "15/03/2026",
      status: "Approved",
      submittedOn: "10/02/2026",
      version: "v1"
    },
    {
      module: "Data Structures and Algorithms",
      faculty: "Prof. Philumon Joseph",
      examDate: "16/03/2026",
      status: "Submitted",
      submittedOn: "12/02/2026",
      version: "v1"
    },
    {
      module: "Data Structures and Algorithms",
      faculty: "Prof. Sangeeth",
      examDate: "17/03/2026",
      status: "Under Scrutiny",
      submittedOn: "13/02/2026",
      version: "v1"
    },
    {
      module: "Data Structures and Algorithms",
      faculty: "Dr. Priya Sharma",
      examDate: "18/03/2026",
      status: "Submitted",
      submittedOn: "14/02/2026",
      version: "v1"
    },
    {
      module: "Data Structures and Algorithms",
      faculty: "Dr. Rajesh Kumar",
      examDate: "19/03/2026",
      status: "Not Submitted",
      submittedOn: "-",
      version: "v1"
    }
  ];

  /* ================= ASSIGN SECTION ================= */

  modules = [
    "Data Structures",
    "Database Management Systems",
    "Operating Systems"
  ];

  subjects: Subject[] = [
    { code: "CS202", name: "Algorithms", module: "Data Structures" },
    { code: "CS203", name: "Trees", module: "Data Structures" },
    { code: "CS301", name: "SQL", module: "Database Management Systems" },
    { code: "CS302", name: "Transactions", module: "Database Management Systems" },
    { code: "CS401", name: "Process Scheduling", module: "Operating Systems" }
  ];

  faculties = [
    "Prof. Sunil KS",
    "Prof. Philumon Joseph",
    "Dr. Midhun Mathew",
    "Dr. Priya Sharma"
  ];

  selectedModule = "";
  selectedSubject = "";
  selectedFaculty = "";

  assignments: any[] = [];

assignSuccess = false;

 assignFaculty() {

  if (!this.selectedModule || !this.selectedSubject || !this.selectedFaculty) {
    alert("Please select Module, Subject and Faculty");
    return;
  }

  const exists = this.assignments.find(
    x => x.subject === this.selectedSubject
  );

  if (exists) {
    alert("Faculty already assigned to this subject");
    return;
  }

  this.assignments.push({
    module: this.selectedModule,
    subject: this.selectedSubject,
    faculty: this.selectedFaculty
  });

  this.assignSuccess = true;

  this.selectedSubject = "";
  this.selectedFaculty = "";
}

  /* ================= DOWNLOAD ================= */

  downloadData = [
    {
      subject: "Data Structures and Algorithms",
      faculty: "Prof. Sunil KS",
      examDate: "15/03/2026"
    },
    {
      subject: "Computer Networks",
      faculty: "Prof. Sangeeth",
      examDate: "26/03/2026"
    }
  ];

  downloadPaper(data: any) {
    alert("Downloading Question Paper PDF");
  }

  viewPdf(data: any) {
    console.log("Viewing PDF:", data.subject);
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

  /* ================= PENDING POPUP ================= */

  showPendingPopup = false;
  popupTitle = "";
  pendingData: any[] = [];

  openPendingPopup(type: string) {

    this.showPendingPopup = true;

    if (type === 'notSubmitted') {

      this.popupTitle = "Papers Not Submitted";

      this.pendingData = this.workflowData.filter(
        x => x.status === "Not Submitted"
      );
    }

    if (type === 'underReview') {

      this.popupTitle = "Papers Under Review";

      this.pendingData = this.workflowData.filter(
        x => x.status === "Under Scrutiny" || x.status === "Submitted"
      );
    }
  }

  closePendingPopup() {
    this.showPendingPopup = false;
  }

  /* ================= NOTIFICATIONS ================= */

  notifications: string[] = [];
  showNotifications = false;

  toggleNotifications() {
    this.showNotifications = !this.showNotifications;
  }
openExam(){
  console.log("Open Examination clicked");

  // later you can open modal or navigate
  // this.router.navigate(['/open-exam']);
}
}