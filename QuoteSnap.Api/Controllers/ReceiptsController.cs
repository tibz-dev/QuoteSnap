using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteSnap.Application.Receipts;
using QuoteSnap.Infrastructure.Services;

namespace QuoteSnap.Api.Controllers;

[ApiController]
[Route("api/receipts")]
[Authorize]
public class ReceiptsController : ControllerBase
{
    private readonly ReceiptService _receiptService;
    private readonly PdfService _pdfService;

    public ReceiptsController(
        ReceiptService receiptService,
        PdfService pdfService)
    {
        _receiptService = receiptService;
        _pdfService = pdfService;
    }

    // GET: /api/receipts
    [HttpGet]
    public async Task<ActionResult<List<ReceiptDto>>> GetAll()
    {
        var receipts =
            await _receiptService.GetAllAsync();

        return Ok(receipts);
    }

    // GET: /api/receipts/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReceiptDto>> GetById(
        Guid id)
    {
        var receipt =
            await _receiptService.GetByIdAsync(id);

        if (receipt is null)
        {
            return NotFound(new
            {
                message = "Receipt not found."
            });
        }

        return Ok(receipt);
    }

    // POST: /api/receipts/from-payment/{paymentId}
    [HttpPost("from-payment/{paymentId:guid}")]
    public async Task<ActionResult<ReceiptDto>> GenerateFromPayment(
        Guid paymentId)
    {
        try
        {
            var receipt =
                await _receiptService.GenerateFromPaymentAsync(
                    paymentId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = receipt.Id },
                receipt);
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
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // GET: /api/receipts/{id}/pdf
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(
        Guid id)
    {
        var result =
            await _pdfService.GenerateReceiptAsync(id);

        if (result is null)
        {
            return NotFound(new
            {
                message = "Receipt not found."
            });
        }

        return File(
            result.Value.Content,
            "application/pdf",
            result.Value.FileName);
    }
}