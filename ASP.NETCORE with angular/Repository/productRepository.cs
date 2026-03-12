using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ASP.NETCORE_with_angular.data;
using ASP.NETCORE_with_angular.model;
using Microsoft.EntityFrameworkCore;

namespace ASP.NETCORE_with_angular.Repository
{
    public class ProductRepository
    {
        private readonly ApplicationDbContext db;
        public ProductRepository(ApplicationDbContext dbContext)
        {
            db = dbContext;
        }
         
        public async Task<List<product>> getAllproduct()
        {
            return await db.products.ToListAsync(); 
        }
        public async Task SaveProduct(product vm)
        {
            await db.products.AddAsync(vm);
            await db.SaveChangesAsync();
        }
        public async Task Updateproduct(int id,product obj)
        {
            var product = await db.products.FindAsync(id);
            if(product==null)
            {
                throw new Exception("product not found");

            }
            product.productname = obj.productname;
            product.price = obj.price;
            product.Description = obj.Description;
            product.Rating = obj.Rating;
            product.status = obj.status;
            await db.SaveChangesAsync();
            

        }
        public async Task Deleteproduct(int id)
        {
            var product= await db.products.FindAsync(id); 
            if(product== null)
            {
                throw new Exception("product not found");
            }
            db.products.Remove(product);
            await db.SaveChangesAsync();
        }
    }





}
