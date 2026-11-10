using AutoPro.Application.Contracts.Customers;
using AutoPro.Domain.Entities;
using AutoPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = "CustomerRepresentative")]
[Route("api/v1/customers")]
public class CustomersController(AutoProDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, new
        {
            customer.Id,
            customer.FullName,
            customer.Phone,
            customer.Email
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<object>> GetById(Guid id, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null) return NotFound();
        return Ok(new { customer.Id, customer.FullName, customer.Phone, customer.Email });
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<object>>> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());
        q = q.Trim();
        var results = await db.Customers.AsNoTracking()
            .Where(c => c.FullName.Contains(q) || c.Phone.Contains(q))
            .OrderBy(c => c.FullName)
            .Take(25)
            .Select(c => new { c.Id, c.FullName, c.Phone })
            .ToListAsync(ct);
        return Ok(results);
    }
}
