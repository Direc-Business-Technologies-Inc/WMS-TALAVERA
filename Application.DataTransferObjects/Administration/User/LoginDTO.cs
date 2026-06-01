using Application.DataTransferObjects.Commons;

namespace Application.DataTransferObjects.Administration.User;

public class LoginDTO : AuditableDTO
{
    public int AttemptCount { get; set; }
    public bool Succeeded { get; set; }
    public Guid AccountId { get; set; }

}
