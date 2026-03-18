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
  selector: 'app-nf-component',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './nf-component.html',
  styleUrls: ['./nf-component.css']
})
export class NfComponent {

  private router = inject(Router);

  userName = "Prof. Sunil KS";
  role = "Faculty";

  selectedMenu = "Dashboard";

  tiles = [
    { title: "Dashboard" },
    { title: "QN Paper" },
    { title: "Scrutiny Report" }
  ];

  navigate(menu: string) {
    this.selectedMenu = menu;
  }

  logout() {
    console.log("Logout clicked");
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

  /* ================= DOWNLOAD / VIEW ================= */

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

  viewPdf(data: any) {
    alert(`Viewing PDF for: ${data.subject}`);
    console.log("Viewing PDF:", data.subject);
  }

  downloadPaper(data: any) {
    alert(`Downloading PDF for: ${data.subject}`);
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

  openExam() {
    console.log("Open Examination clicked");
    // Navigate or open modal later
    // this.router.navigate(['/open-exam']);
  }
}