import { Routes } from '@angular/router';
import { ItemMaster } from './component/item-master/item-master';
import { LoginComponent } from './component/login-component/login-component';
import { HomeComponent } from './component/home-component/home-component';
import { FacultyComponent } from './component/faculty-component/faculty-component';
import { ModuleComponent } from './component/module-component/module-component';
import { EcComponent } from'./component/ec-component/ec-component';
import { NfComponent } from './component/nf-component/nf-component';
import { McComponent } from './component/mc-component/mc-component';

export const routes: Routes = [
    {path: "",component:LoginComponent},
    {path:"item-master",component:ItemMaster},
    {path:"home",component:HomeComponent},
    {path:"faculty",component:FacultyComponent},
 {path:"module",component:ModuleComponent},
 {path:"ec",component:EcComponent},
 {path:"nf",component:NfComponent},
 {path:"mc",component:McComponent}
];
