using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Enums;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;

namespace FinancialSystem.Application.Services.EnvironmentServices
{
    public class TransactionAppService
    {
        private readonly IGeneralRepository<PlannedExpensesAndProfits> _plannedTransactionsRepository;
        private readonly IGeneralRepository<UnplannedExpensesAndProfits> _unplannedTransactionsRepository;

        public TransactionAppService(IGeneralRepository<PlannedExpensesAndProfits> plannedTransactionsRepository,
                                     IGeneralRepository<UnplannedExpensesAndProfits> unplannedTransactionsRepository)
        {
            _plannedTransactionsRepository = plannedTransactionsRepository;
            _unplannedTransactionsRepository = unplannedTransactionsRepository;
        }

        public async Task InsertPlannedTransaction(TransactionDto input)
        {
            var teste = new PlannedExpensesAndProfits
            {
                Amount = input.Amount,
                Description = input.Description,
                EnvironmentId = Guid.Empty,
                Id = Guid.NewGuid(),
                Type = input.Type,
                TransactionDate = input.TransactionDate,
                RecurrenceType = input.RecurrenceType
            };

            await _plannedTransactionsRepository.InsertAsync(teste);
        }

        public async Task InsertUnplannedTransaction(TransactionDto input)
        {
            var teste = new UnplannedExpensesAndProfits
            {
                Amount = input.Amount,
                Description = input.Description,
                EnvironmentId = Guid.Empty,
                Id = Guid.NewGuid(),
                Type = input.Type,
                TransactionDate = input.TransactionDate
            };

            await _unplannedTransactionsRepository.InsertAsync(teste);
        }
    }

    public class TransactionDto
    {
        public FinancialRecordTypeEnum Type { get; set; }
        public RecurrenceTypeEnum RecurrenceType { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public DateTime TransactionDate { get; set; }  //data que a transação irá ocorrer se for planejada
    }
}
