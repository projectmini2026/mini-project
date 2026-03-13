import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { product } from '../Model/product';

@Injectable({
  providedIn: 'root',
})
export class ProductService {
    private apiurl ='https://localhost:7196/api/Products';
  constructor(){}
  http = inject(HttpClient)
  getallproduct(){
    return this.http.get<product[]>(this.apiurl)
  }
  addproduct(data:any){
    return this.http.post(this.apiurl,data)
  }
  updateproduct(product:product){

    return this.http.put(`${this.apiurl}/${product.id}`,product)
  }
  deleteproduct(id:number)
  {
    return this.http.delete(`${this.apiurl}/${id}`)
  }
}
