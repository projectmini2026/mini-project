import { Component } from '@angular/core';
import { BaseComponent } from '../base.component';
import { TableRow } from '../../utilities';

@Component({
  selector: 'app-faculty-component',
  imports: [],
  templateUrl: './faculty-component.html',
  styleUrl: './faculty-component.css',
})

export class FacultyComponent extends BaseComponent{
  
  columns = [
    { field: 'id', header: 'ID' },
    { field: 'name', header: 'Name' },
    { field: 'role', header: 'Role' },
    { field: 'department', header: 'Department' }
  ];

  rows: TableRow[] = [
    { id: 1, name: 'Anil', role: 'HOD', department: 'CSE' },
    { id: 2, name: 'Rahul', role: 'Faculty', department: 'CSE' },
    { id: 3, name: 'Meera', role: 'Coordinator', department: 'IT' },
    { id: 4, name: 'Arjun', role: 'Scrutinizer', department: 'ECE' }
  ];

}


