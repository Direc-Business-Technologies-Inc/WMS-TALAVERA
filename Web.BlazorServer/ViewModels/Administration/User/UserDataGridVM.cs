namespace Web.BlazorServer.ViewModels.Administration.User;

public class UserDataGridVM
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Position { get; set; }
    public bool Active { get; set; }
}
