using InternalExamScrutinySystem.Api.Data;

namespace InternalExamScrutinySystem.Api.Helpers;

public static class PositionExtensions
{
    public static string? ToShortForm(this Position? position)
    {
        if (position == null) return null;
        return position switch
        {
            Position.Professor => "Prof.",
            Position.AssociateProfessor => "Assoc. Prof.",
            Position.AssistantProfessor => "Asst. Prof.",
            Position.GuestLecturer => "Guest Lec.",
            Position.Doctorate => "Dr.",
            _ => null
        };
    }
}
