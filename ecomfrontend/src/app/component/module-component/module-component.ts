import { Component } from '@angular/core';
import { TableRow } from '../../utilities';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-module-component',
  imports: [],
  templateUrl: './module-component.html',
  styleUrl: './module-component.css',
})
export class ModuleComponent {
 columns = [
    { field: 'ModuleId', header: 'ID' },
    { field: 'ModuleName', header: 'Subject' },
    { field: 'Noofsubjects', header: 'No of subjects' },
    
  ];

  rows: TableRow[] = [
    { ModuleId: 1, ModuleName: 'programming and problem solving',Noofsubjects: '4'},
    { ModuleId: 2, ModuleName: 'Core Computer Systems', Noofsubjects: '5'},
    { ModuleId: 3, ModuleName: 'Data & Information Systems', Noofsubjects: '3'},
    { ModuleId: 4, ModuleName: 'Graphics,Networks & Advanced Topics', Noofsubjects: '4'}
  ];

}


