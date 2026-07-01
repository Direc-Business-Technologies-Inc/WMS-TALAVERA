namespace Web.BlazorServer.ViewModels.Others;

public class EmployeeNsVM
{
    public int NsId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public int NsDepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int NsSubsidiaryId { get; set; }
    public string SubsidiaryName { get; set; } = string.Empty;
}
