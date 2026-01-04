using FluentValidation;

namespace BoltTickets.Application.Bookings.Commands;

public class BookTicketCommandValidator : AbstractValidator<BookTicketCommand>
{
    public BookTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
