namespace FinancialSystem.Application.Shared.Interfaces
{
    public interface IAppSession
    {
        long? UserId { get; }
        Guid? EnvironmentId { get; set; }
    }
}