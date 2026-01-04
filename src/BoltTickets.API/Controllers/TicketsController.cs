using System;
using System.Threading.Tasks;
using BoltTickets.Application.Bookings.Commands;
using BoltTickets.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using BoltTickets.API.Hubs;
using BoltTickets.Domain.Repositories;
using BoltTickets.Domain.Entities;

namespace BoltTickets.API.Controllers;

/// <summary>
/// API Controller for Ticket operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITicketCacheService _ticketCache;
    private readonly IHubContext<TicketHub> _hubContext;

    private readonly ITicketRepository _ticketRepository;

    public TicketsController(IMediator mediator, ITicketCacheService ticketCache, IHubContext<TicketHub> hubContext, ITicketRepository ticketRepository)
    {
        _mediator = mediator;
        _ticketCache = ticketCache;
        _hubContext = hubContext;
        _ticketRepository = ticketRepository;
    }

    /// <summary>
    /// Processes a ticket purchase request.
    /// This is a high-speed endpoint that offloads persistence to Kafka.
    /// </summary>
    /// <param name="request">Booking details.</param>
    /// <returns>HTTP 202 Accepted with Booking ID.</returns>
    [HttpPost("book")]
    public async Task<IActionResult> BookTicket([FromBody] BookTicketRequest request)
    {
        var command = new BookTicketCommand(request.TicketId, request.UserId);
        var bookingId = await _mediator.Send(command);
        
        return Accepted(new { BookingId = bookingId, Message = "Booking processing" });
    }

    [HttpGet("inventory/{ticketId}")]
    public async Task<IActionResult> GetInventory(Guid ticketId)
    {
        var count = await _ticketCache.GetAvailableCountAsync(ticketId);
        return Ok(new { TicketId = ticketId, AvailableCount = count });
    }

    /// <summary>
    /// Initializes ticket inventory for testing purposes.
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> SeedTicket([FromBody] SeedTicketRequest request)
    {
        // 1. Initialize SQL Database
        var ticket = await _ticketRepository.GetAsync(request.TicketId, default);
        if (ticket == null)
        {
            ticket = new Ticket(request.TicketId, "Flash Sale Event", request.Count);
            await _ticketRepository.AddAsync(ticket, default);
        }
        else
        {
             // Update existsing if needed? Or just skip.
        }
        
        // 2. Initialize Redis
        await _ticketCache.InitializeCounterAsync(request.TicketId, request.Count);
        
        await _hubContext.Clients.All.SendAsync("inventoryupdated", new { TicketId = request.TicketId, AvailableCount = request.Count });

        return Ok($"Ticket {request.TicketId} initialized in both DB and Redis with {request.Count}");
    }
}

public record BookTicketRequest(Guid TicketId, Guid UserId);
public record SeedTicketRequest(Guid TicketId, int Count);
