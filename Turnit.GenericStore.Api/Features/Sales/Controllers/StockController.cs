using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using Turnit.GenericStore.Api.Entities;

namespace Turnit.GenericStore.Api.Features.Sales;

[Route("stock")]
public class StockController : ApiControllerBase
{
    private readonly ISession _session;
    public StockController(ISession session)
    {
        _session = session;
    }

    [HttpGet, Route("/stores")]
    public async Task<ActionResult<StoreModel[]>> GetStores()
    {
        try
        {
            var transaction = _session.BeginTransaction();
            var stores = await _session.QueryOver<Store>().ListAsync();
            var result = stores.Select(x => new StoreModel
            {
                Id = x.Id,
                Name = x.Name
            })
                .ToArray();
            return result;

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return StatusCode(500);
        }
    }





    [HttpPost, Route("/store/{storeId}/restock")]
    public async Task<IActionResult> RestockProducts([FromBody] RestockModel[] restockParams, Guid storeId)
    {
        using (var transaction = _session.BeginTransaction())
            try
            {
                var store = await _session.GetAsync<Store>(storeId);
                if (store is null) { return NotFound("Store"); }
                foreach (var restock in restockParams)
                {
                    var product = await _session.GetAsync<Product>(restock.productId);
                    // get or create new productAvailability entity
                    var productAvailability = await _session.QueryOver<ProductAvailability>().Where(x => x.Product == product && x.Store == store).SingleOrDefaultAsync<ProductAvailability>() ?? new ProductAvailability
                    {
                        Id = Guid.NewGuid(),
                        Product = product,
                        Store = store,
                        Availability = 0
                    };
                    productAvailability.Availability += restock.Quantity;
                    await _session.SaveOrUpdateAsync(productAvailability);
                }

                // if no exception thrown save all changes
                transaction.Commit();
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