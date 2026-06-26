using Ardalis.GuardClauses;

namespace Domain.ValueObjects.Others;

public class EmployeeNsVO : ValueObject
{
    public int? NsId { get; private set; }
    public string EmployeeCode { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public int? NsDepartmentId { get; private set; }
    public string DepartmentName { get; private set; } = string.Empty;
    public int? NsSubsidiaryId { get; private set; }
    public string SubsidiaryName { get; private set; } = string.Empty;

    EmployeeNsVO() { }

    public EmployeeNsVO(
        int nsId,
        string employeeCode,
        string firstName,
        string lastName,
        int nsDepartmentId,
        string departmentName,
        int nsSubsidiaryId,
        string subsidiaryName)
    {
        NsId = Guard.Against.NegativeOrZero(nsId, nameof(nsId), "NetSuite employee ID cannot be negative or zero");
        EmployeeCode = Guard.Against.NullOrEmpty(employeeCode, nameof(employeeCode), "Employee code cannot be null or empty");
        FirstName = firstName ?? string.Empty;
        LastName = lastName ?? string.Empty;
        NsDepartmentId = nsDepartmentId;
        DepartmentName = departmentName ?? string.Empty;
        NsSubsidiaryId = nsSubsidiaryId;
        SubsidiaryName = subsidiaryName ?? string.Empty;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return NsId ?? 0;
        yield return EmployeeCode;
        yield return FirstName;
        yield return LastName;
        yield return NsDepartmentId ?? 0;
        yield return DepartmentName;
        yield return NsSubsidiaryId ?? 0;
        yield return SubsidiaryName;
    }
}
