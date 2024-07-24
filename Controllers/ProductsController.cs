using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BasicCrudDemo.Models;
using CsvHelper;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace CrudDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductsContext _context;

        public ProductsController(ProductsContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetProducts(
            string sortField = "ProductId",
            string sortOrder = "asc",
            string nameFilter = "",
            int pageNumber = 1)
        {
            const int pageSize = 10;
            var products = await _context.SearchProductsAsync(sortField, sortOrder, nameFilter, pageNumber, pageSize);
            return products.Select(p => new ProductDTO
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Price = p.Price,
                Provider = p.Provider
            }).ToList();
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDTO>> GetProduct(int id)
        {
            var product = await _context.GetProductByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return new ProductDTO
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Price = product.Price,
                Provider = product.Provider
            };
        }

        [HttpGet("{id}/stores")]
        public async Task<ActionResult<IEnumerable<StoreDTO>>> GetStoresForProduct(int id)
        {
            var stores = await _context.ProductStores
                .Where(ps => ps.ProductId == id)
                .Select(ps => new StoreDTO
                {
                    StoreId = ps.Store.StoreId,
                    Name = ps.Store.Name
                }).ToListAsync();

            if (stores == null || !stores.Any())
            {
                return NotFound();
            }

            return stores;
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<ProductDTO>> PostProduct(ProductDTO productDTO)
        {
            var product = new Product
            {
                Name = productDTO.Name,
                Price = productDTO.Price,
                Provider = productDTO.Provider
            };

            product.ProductId = await _context.AddProductAsync(product);
            if (productDTO.StoreIds != null && productDTO.StoreIds.Any())
            {
                foreach (var storeId in productDTO.StoreIds)
                {
                    _context.ProductStores.Add(new ProductStore
                    {
                        ProductId = product.ProductId,
                        StoreId = storeId
                    });
                }
                await _context.SaveChangesAsync();
            }

            var createdProductDTO = new ProductDTO
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Price = product.Price,
                Provider = product.Provider,
                StoreIds = productDTO.StoreIds
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, createdProductDTO);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadProducts(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty or not provided.");

            using (var stream = new StreamReader(file.OpenReadStream()))
            {
                using (var csv = new CsvReader(stream, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<ProductDTO>().ToList();

                    foreach (var record in records)
                    {
                        await PostProduct(record);
                    }
                }
            }

            return Ok("File processed successfully.");
        }



        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, ProductDTO productDTO)
        {
            if (id != productDTO.ProductId)
            {
                return BadRequest();
            }

            var product = new Product
            {
                ProductId = productDTO.ProductId,
                Name = productDTO.Name,
                Price = productDTO.Price,
                Provider = productDTO.Provider
            };

            await _context.UpdateProductAsync(product);

            // Update store associations
            var existingStores = _context.ProductStores.Where(ps => ps.ProductId == id);
            _context.ProductStores.RemoveRange(existingStores);

            if (productDTO.StoreIds != null && productDTO.StoreIds.Any())
            {
                foreach (var storeId in productDTO.StoreIds)
                {
                    _context.ProductStores.Add(new ProductStore
                    {
                        ProductId = product.ProductId,
                        StoreId = storeId
                    });
                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            await _context.DeleteProductAsync(id);
            return NoContent();
        }

        [HttpGet("stores/{id}")]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetStoreProducts(int id)
        {
            Console.WriteLine("GetStoreProducts");
            var store = await _context.Stores
                .Include(s => s.ProductStores)
                .ThenInclude(ps => ps.Product)
                .FirstOrDefaultAsync(s => s.StoreId == id);

            if (store == null)
            {
                return NotFound();
            }

            var productDTOs = store.ProductStores.Select(ps => new ProductDTO
            {
                ProductId = ps.Product.ProductId,
                Name = ps.Product.Name,
                Price = ps.Product.Price,
                Provider = ps.Product.Provider
            });

            return Ok(productDTOs);
        }
    }
}
