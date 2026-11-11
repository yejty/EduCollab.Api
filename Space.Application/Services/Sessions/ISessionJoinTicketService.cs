using Space.Application.Models;

namespace Space.Application.Services.Sessions
{
    public interface ISessionJoinTicketService
    {
        SessionJoinTicketResult CreateTicket(
            LiveSession session,
            string subject,
            string displayName,
            string role,
            string colyseusRole);
    }
}
