using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using Turnit.GenericStore.Api.Entities;

namespace Turnit.GenericStore.Api.Features.Sales;

[Route("categories")]
public class CategoriesController : ApiControllerBase
{
    private readonly ISession _session;

    public CategoriesController(ISession session)
    {
        _session = session;
    }

    [HttpGet, Route("")]
    public async Task<ActionResult<CategoryModel[]>> AllCategories()
    {
        try
        {
            var categories = await _session.QueryOver<Category>().ListAsync();
            var result = categories
                .Select(x => new CategoryModel
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
}