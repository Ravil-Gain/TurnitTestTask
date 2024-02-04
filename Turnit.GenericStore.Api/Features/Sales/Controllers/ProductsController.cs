using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using Turnit.GenericStore.Api.Entities;

namespace Turnit.GenericStore.Api.Features.Sales;

[Route("products")]
public class ProductsController : ApiControllerBase
{
    private readonly ISession _session;

    public ProductsController(ISession session)
    {
        _session = session;
    }

    [HttpGet, Route("by-category/{categoryId:guid}")]
    public async Task<ActionResult<ProductModel[]>> ProductsByCategory(Guid categoryId)
    {
        try
        {
            var result = new List<ProductModel>();
            var products = await _session.QueryOver<ProductCategory>()
                .Where(x => x.Category.Id == categoryId)
                .Select(x => x.Product)
                .ListAsync<Product>();


            // take whole availability data with one request
            var productsAvailability = await _session.QueryOver<ProductAvailability>()
                .WhereRestrictionOn(c => c.Product.Id).IsIn(products.Select(p => p.Id).ToList())
                .ListAsync();

            foreach (var product in products)
            {
                var model = new ProductModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Availability = productsAvailability
                        .Where(x => x.Product.Id == product.Id)
                        .Select(x => new AvailabilityModel
                        {
                            StoreId = x.Store.Id,
                            Availability = x.Availability
                        }).ToArray()
                };
                result.Add(model);
            }

            return result.ToArray();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return StatusCode(500);
        }
    }

    [HttpGet, Route("")]
    public async Task<ActionResult<ProductCategoryModel[]>> AllProducts()
    {
        try
        {
            var products = await _session.QueryOver<Product>().ListAsync<Product>();
            var productCategories = await _session.QueryOver<ProductCategory>().ListAsync();

            var productModels = new List<ProductModel>();
            // take whole availability data with one request
            var productsAvailability = await _session.QueryOver<ProductAvailability>()
                .WhereRestrictionOn(c => c.Product.Id).IsIn(products.Select(p => p.Id).ToList())
                .ListAsync();

            foreach (var product in products)
            {
                var model = new ProductModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Availability = productsAvailability
                        .Where(x => x.Product.Id == product.Id)
                        .Select(x => new AvailabilityModel
                        {
                            StoreId = x.Store.Id,
                            Availability = x.Availability
                        }).ToArray()
                };
                productModels.Add(model);
            }

            var result = new List<ProductCategoryModel>();
            foreach (var category in productCategories.GroupBy(x => x.Category.Id))
            {
                var productIds = category.Select(x => x.Product.Id).ToHashSet();
                result.Add(new ProductCategoryModel
                {
                    CategoryId = category.Key,
                    Products = productModels
                        .Where(x => productIds.Contains(x.Id))
                        .ToArray()
                });
            }

            var uncategorizedProducts = productModels.Except(result.SelectMany(x => x.Products));
            if (uncategorizedProducts.Any())
            {
                result.Add(new ProductCategoryModel
                {
                    Products = uncategorizedProducts.ToArray()
                });
            }

            return result.ToArray();

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return StatusCode(500);
        }
    }
}