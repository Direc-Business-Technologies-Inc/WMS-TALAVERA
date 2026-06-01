namespace Web.BlazorServer.ViewModels.Others;

public class PersonNameVM
{
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;


    public bool Validate() => !string.IsNullOrEmpty(FirstName) || !string.IsNullOrEmpty(LastName);
    public string ToFullName()
    {
        FullName = $"{FirstName} {(string.IsNullOrEmpty(MiddleName) ? string.Empty : $" {MiddleName}")} {LastName}";

        return FullName;

    }
}
