using FinancialSystem.Application.Shared.Dtos.Environment;

namespace FinancialSystem.Application.Shared.Interfaces.EnvironmentServices
{
    public interface ITransactionAppService
    {
        Task InsertPlannedTransaction(TransactionDataDto input);
        Task InsertUnplannedTransaction(TransactionDataDto input);
        Task UpdatePlannedTransaction(TransactionDataDto input);
        Task UpdateUnplannedTransaction(TransactionDataDto input);
        Task<List<TransactionDataDto>> GetAllPlannedTransactions();
        Task<List<TransactionDataDto>> GetAllUnplannedTransactions();
        Task DeleteTransaction(Guid transactionId);
    }
}
