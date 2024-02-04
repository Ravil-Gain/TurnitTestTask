using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using Turnit.GenericStore.Api.Entities;

namespace Turnit.GenericStore.Api.Features.Sales;

[Route("productsAvalability")]
public class ProductAvailabilityController : ApiControllerBase
{
    private readonly ISession _session;
    public ProductAvailabilityController(ISession session)
    {
        _session = session;
    }

    [HttpPost, Route("/products/{productId}/book")]
    public async Task<IActionResult> BookProducts([FromBody] ProductBookingModel[] bookingParams, Guid productId)
    {
        // assume that bookings should be made all together, not partially
        using (var transaction = _session.BeginTransaction())
            try
            {
                var product = await _session.GetAsync<Product>(productId);
                if (product is null) { return NotFound("product"); }

                foreach (var booking in bookingParams)
                {
                    var store = await _session.GetAsync<Store>(booking.storeId);
                    if (store is null) { return NotFound("store, id = " + booking.storeId); }

                    var productAvailability = await _session.QueryOver<ProductAvailability>().Where(x => x.Product == product && x.Store == store).SingleOrDefaultAsync<ProductAvailability>();
                    if (booking.Quantity > productAvailability.Availability)
                    {
                        throw new Exception(productAvailability.Store.Name + " store dont have enougth products");
                    }
                    else
                    {
                        productAvailability.Availability -= booking.Quantity;
                    }

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
        // guess better to send an updated object instead of ok response
        return Ok();
    }

}