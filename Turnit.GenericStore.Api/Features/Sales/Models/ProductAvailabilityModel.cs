using System;

namespace Turnit.GenericStore.Api.Features.Sales;

public class ProductBookingModel
{
    public Guid storeId { get; set; }

    public int Quantity { get; set; }
}


public class RestockModel
{
    public Guid productId { get; set; }

    public int Quantity { get; set; }
}