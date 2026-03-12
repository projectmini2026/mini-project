using System.Threading.Tasks;
using ASP.NETCORE_with_angular.model;
using ASP.NETCORE_with_angular.Repository;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NETCORE_with_angular.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductRepository prodt;

        public ProductsController(ProductRepository ProductRepository)
        {
            this.prodt = ProductRepository;
        }
        [HttpGet]
        public async Task<ActionResult>ProductList()
        {
            var allProduct = await prodt.getAllproduct();
            return Ok(allProduct);
        }
        [HttpPost]
        public async Task<ActionResult> AddProduct(product vm)
        {
            await prodt.SaveProduct(vm);
            return Ok(vm);
        }
        [HttpPut("{id}")]
        public async Task<ActionResult> Updateproduct(int id, [FromBody]product vm)
        {
            await prodt.Updateproduct(id, vm);
            return Ok(vm);
        }
        [HttpDelete("{id}")]
        public async Task <ActionResult>Deleteproduct(int id)
        {
            await prodt.Deleteproduct(id);
            return Ok();
        }
    }
}
