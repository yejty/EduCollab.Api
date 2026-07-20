namespace EduCollab.Contracts.Responses.Sessions
{
    public class SessionJoinTicketResponse
    {
        public string JoinTicket { get; set; } = string.Empty;

        public int SessionId { get; set; }

        public string? ColyseusEndpoint { get; set; }

        public string Role { get; set; } = string.Empty;

        public string ColyseusRole { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }
    }
}
