namespace Application.DataTransferObjects.Others.NS;

public class EmployeeDTO
{
    public int NetsuiteEmployeeInternalId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
