import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, inject, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { product } from '../../Model/product';
import { ProductService } from '../../Services/product-service';
import { BaseComponent } from '../base.component';

@Component({
  selector: 'app-item-master',
  imports: [CommonModule ,ReactiveFormsModule, FormsModule ],
  templateUrl: './item-master.html',
  styleUrl: './item-master.css',
})
export class ItemMaster extends BaseComponent implements OnInit { 
  @ViewChild('myModal') modal :ElementRef | undefined;
productForm:FormGroup =new FormGroup({})
productlist :product[]=[];
formvalue:any;
prodservice = inject(ProductService);
private cd = inject(ChangeDetectorRef);
constructor(private fb:FormBuilder){
  super();
}
ngOnInit(): void {
  this.setFormState()
  this.getproduct()
}
  openModal(){
    const prodModal=document.getElementById('myModal');
    if(prodModal!=null){
      prodModal.style.display='block';
    }
  }
  closeModal(){
   this. setFormState()
    if(this.modal !=null){
  
    
      this.modal.nativeElement.style.display='none'
  }

}

setFormState(){
  this.productForm = this.fb.group({
    id:0,
    productname :['',[Validators.required]],
    price :['',[Validators.required]],
    description :['',[Validators.required]],
    rating :['',[Validators.required]],
    status :[false,[Validators.required]],
  })
}
onsubmit(){
  console.log(this.productForm.value)
  if(this.productForm.invalid){
    alert("please fill alll records");
    return;
  }
  else{
    this.formvalue=this.productForm.value;
    this.prodservice.addproduct(this.formvalue).subscribe((res)=>{
      alert("product addedsuccessfully");
      this.productForm.reset();
      this.getproduct();
      this.closeModal();
    })
  }
}
getproduct(){
  this.prodservice.getallproduct().subscribe((res)=>{
    this.productlist = res ;
this.cd.detectChanges();
  })
}
 onEdit(product: product){
  this.openModal()
  this.productForm.patchValue(product)

 }


onDelete(id:number){
  const isConfirm =confirm("are you sure wantto delete this record?");
  if(isConfirm){
  this.prodservice.deleteproduct(id).subscribe((res)=>{
    alert("product deleted successfully");
    this.getproduct();
  })
}else{
  alert("you select no option")
}
}
}
