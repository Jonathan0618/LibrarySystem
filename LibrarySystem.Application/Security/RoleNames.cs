namespace LibrarySystem.Application.Security;

public static class RoleNames
{
    public const string Administrator = "Administrator";
    public const string Librarian = "Librarian";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    public static readonly IReadOnlyCollection<string> All =
    [
        Administrator,
        Librarian,
        Teacher,
        Student
    ];
}
