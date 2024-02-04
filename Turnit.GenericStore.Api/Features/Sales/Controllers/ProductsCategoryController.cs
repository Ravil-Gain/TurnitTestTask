using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using Turnit.GenericStore.Api.Entities;

namespace Turnit.GenericStore.Api.Features.Sales;

[Route("products/{productId:guid}")]
public class ProductsCategoryController : ApiControllerBase
{
    private readonly ISession _session;

    public ProductsCategoryController(ISession session)
    {
        _session = session;
    }

    [HttpPut, Route("category/{categoryId:guid}")]
    public async Task<IActionResult> CategoryToProduct(Guid productId, Guid categoryId)
    {
        using (var transaction = _session.BeginTransaction())
            try
            {
                var product = await _session.QueryOver<Product>().Where(x => x.Id == productId).SingleOrDefaultAsync<Product>();
                // if (product is null) { throw new Exception("no such product"); }
                if (product is null) { return NotFound("product"); }
                var category = await _session.QueryOver<Category>().Where(x => x.Id == categoryId).SingleOrDefaultAsync<Category>();
                if (category is null) { throw new Exception("no such category"); }

                var productCategory = await _session.QueryOver<ProductCategory>().Where(x => x.Product.Id == productId && x.Category.Id == categoryId).SingleOrDefaultAsync<Product>();
                if (productCategory is null)
                {
                    _session.Save(new ProductCategory { Id = Guid.NewGuid(), Product = product, Category = category });
                    transaction.Commit();
                }
            }
            catch (Exception e)
            {
                transaction?.Rollback();
                Console.WriteLine(e.Message);
                return StatusCode(500);
            }
        return Ok();
    }

    [HttpDelete, Route("category/{categoryId:guid}")]
    public async Task<IActionResult> CategoryFromProduct(Guid productId, Guid categoryId)
    {
        using (var transaction = _session.BeginTransaction())
            try
            {
                var product = await _session.GetAsync<Product>(productId);
                if (product is null) { return NotFound("product"); }

                var productCategory = await _session.QueryOver<ProductCategory>()
                    .Where(x => x.Product.Id == productId && x.Category.Id == categoryId)
                    .SingleOrDefaultAsync<ProductCategory>();
                if (productCategory is not null)
                {
                    _session.Delete(productCategory);
                    transaction.Commit();
                }
            }
            catch (Exception e)
            {
                transaction?.Rollback();
                Console.WriteLine(e.Message);
                return StatusCode(500);
            }
        return Ok();
    }
}