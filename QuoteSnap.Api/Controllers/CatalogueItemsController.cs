using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteSnap.Application.CatalogueItems;
using QuoteSnap.Infrastructure.Services;

namespace QuoteSnap.Api.Controllers;

[ApiController]
[Route("api/catalogue-items")]
[Authorize]
public class CatalogueItemsController : ControllerBase
{
    private readonly CatalogueItemService _catalogueItemService;

    public CatalogueItemsController(
        CatalogueItemService catalogueItemService)
    {
        _catalogueItemService = catalogueItemService;
    }

    // GET: /api/catalogue-items
    [HttpGet]
    public async Task<ActionResult<List<CatalogueItemDto>>> GetAll()
    {
        var items =
            await _catalogueItemService.GetAllAsync();

        return Ok(items);
    }

    // GET: /api/catalogue-items/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CatalogueItemDto>> GetById(
        Guid id)
    {
        var item =
            await _catalogueItemService.GetByIdAsync(id);

        if (item is null)
        {
            return NotFound(new
            {
                message = "Catalogue item not found."
            });
        }

        return Ok(item);
    }

    // POST: /api/catalogue-items
    [HttpPost]
    public async Task<ActionResult<CatalogueItemDto>> Create(
        CreateCatalogueItemRequest request)
    {
        try
        {
            var item =
                await _catalogueItemService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = item.Id },
                item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // PUT: /api/catalogue-items/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CatalogueItemDto>> Update(
        Guid id,
        UpdateCatalogueItemRequest request)
    {
        try
        {
            var item =
                await _catalogueItemService.UpdateAsync(
                    id,
                    request);

            if (item is null)
            {
                return NotFound(new
                {
                    message = "Catalogue item not found."
                });
            }

            return Ok(item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // DELETE: /api/catalogue-items/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted =
            await _catalogueItemService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Catalogue item not found."
            });
        }

        return NoContent();
    }
}