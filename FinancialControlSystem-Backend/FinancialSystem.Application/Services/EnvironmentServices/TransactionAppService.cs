using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Enums;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;

namespace FinancialSystem.Application.Services.EnvironmentServices
{
    public class TransactionAppService : AppServiceBase, ITransactionAppService
    {
        private readonly IGeneralRepository<PlannedExpensesAndProfits> _plannedTransactionsRepository;
        private readonly IGeneralRepository<UnplannedExpensesAndProfits> _unplannedTransactionsRepository;
        private readonly TimeZoneInfo _tzBrasilia;

        public TransactionAppService(IAppSession appSession,
                                     IGeneralRepository<PlannedExpensesAndProfits> plannedTransactionsRepository,
                                     IGeneralRepository<UnplannedExpensesAndProfits> unplannedTransactionsRepository) : base(appSession)
        {
            _plannedTransactionsRepository = plannedTransactionsRepository;
            _unplannedTransactionsRepository = unplannedTransactionsRepository;
            _tzBrasilia = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }

        #region InsertPlannedTransaction
        public async Task InsertPlannedTransaction(TransactionDataDto input)
        {
            try
            {
                var transaction = new PlannedExpensesAndProfits
                {
                    Amount = input.Amount,
                    Description = input.Description,
                    EnvironmentId = (Guid)EnvironmentId,
                    Id = Guid.NewGuid(),
                    Type = (FinancialRecordTypeEnum)input.Type,
                    TransactionDate = TimeZoneInfo.ConvertTimeToUtc(input.TransactionDate, _tzBrasilia),
                    RecurrenceType = (RecurrenceTypeEnum)input.RecurrenceType
                };

                await _plannedTransactionsRepository.InsertAsync(transaction);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region InsertUnplannedTransaction
        public async Task InsertUnplannedTransaction(TransactionDataDto input)
        {
            try
            {
                var unplannedTransaction = new UnplannedExpensesAndProfits
                {
                    Amount = input.Amount,
                    Description = input.Description,
                    EnvironmentId = (Guid)EnvironmentId,
                    Id = Guid.NewGuid(),
                    Type = (FinancialRecordTypeEnum)input.Type,
                    TransactionDate = TimeZoneInfo.ConvertTimeToUtc(input.TransactionDate, _tzBrasilia)
                };

                await _unplannedTransactionsRepository.InsertAsync(unplannedTransaction);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region UpdatePlannedTransaction
        public async Task UpdatePlannedTransaction(TransactionDataDto input)
        {
            try
            {
                if (input.Id == null || input.Id == Guid.Empty)
                    throw new Exception("Identificador de transação recebido!");

                var plannedTransaction = await _plannedTransactionsRepository.FirstOrDefaultAsync(x => x.Id == input.Id);

                if (plannedTransaction == null)
                    throw new Exception("Transação não encontrada!");

                plannedTransaction.Amount = input.Amount;
                plannedTransaction.RecurrenceType = (RecurrenceTypeEnum)input.RecurrenceType;
                plannedTransaction.Description = input.Description;
                plannedTransaction.TransactionDate = TimeZoneInfo.ConvertTimeToUtc(input.TransactionDate, _tzBrasilia);
                plannedTransaction.Type = (FinancialRecordTypeEnum)input.Type;

                await _plannedTransactionsRepository.UpdateAsync(plannedTransaction);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region UpdateUnplannedTransaction
        public async Task UpdateUnplannedTransaction(TransactionDataDto input)
        {
            try
            {
                if (input.Id == null || input.Id == Guid.Empty)
                    throw new Exception("Identificador de transação recebido!");

                var unplannedTransaction = await _unplannedTransactionsRepository.FirstOrDefaultAsync(x => x.Id == input.Id);

                if (unplannedTransaction == null)
                    throw new Exception("Transação não encontrada!");

                unplannedTransaction.Amount = input.Amount;
                unplannedTransaction.Description = input.Description;
                unplannedTransaction.TransactionDate = TimeZoneInfo.ConvertTimeToUtc(input.TransactionDate, _tzBrasilia);
                unplannedTransaction.Type = (FinancialRecordTypeEnum)input.Type;

                await _unplannedTransactionsRepository.UpdateAsync(unplannedTransaction);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region GetAllPlannedTransactions
        public async Task<List<TransactionDataDto>> GetAllPlannedTransactions()
        {
            try
            {
                var plannedTransactions = await _plannedTransactionsRepository
                                                .GetAll()
                                                .Where(x => x.EnvironmentId == (Guid)EnvironmentId &&
                                                           !x.IsDeleted)
                                                .ToListAsync();

                if (plannedTransactions.Count == 0)
                    return new List<TransactionDataDto>();

                var outputList = new List<TransactionDataDto>();

                plannedTransactions.ForEach(transaction =>
                {
                    outputList.Add(new TransactionDataDto
                    {
                        Amount = transaction.Amount,
                        Description = transaction.Description,
                        Id = transaction.Id,
                        RecurrenceType = (int)transaction.RecurrenceType,
                        TransactionDate = transaction.TransactionDate,
                        Type = (int)transaction.Type
                    });
                });

                return outputList;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region GetAllUnplannedTransactions
        public async Task<List<TransactionDataDto>> GetAllUnplannedTransactions()
        {
            try
            {
                var unplannedTransactions = await _unplannedTransactionsRepository
                                                  .GetAll()
                                                  .Where(x => x.EnvironmentId == (Guid)EnvironmentId &&
                                                             !x.IsDeleted)
                                                  .ToListAsync();

                if (unplannedTransactions.Count == 0)
                    return new List<TransactionDataDto>();

                var outputList = new List<TransactionDataDto>();

                unplannedTransactions.ForEach(transaction =>
                {
                    outputList.Add(new TransactionDataDto
                    {
                        Amount = transaction.Amount,
                        Description = transaction.Description,
                        Id = transaction.Id,
                        TransactionDate = transaction.TransactionDate,
                        Type = (int)transaction.Type
                    });
                });

                return outputList;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region DeleteTransaction
        public async Task DeleteTransaction(Guid transactionId)
        {
            try
            {
                var plannedTransaction = await _plannedTransactionsRepository.GetByIdAsync(transactionId);

                UnplannedExpensesAndProfits unplannedTransaction = null;

                if (plannedTransaction == null)
                    unplannedTransaction = await _unplannedTransactionsRepository.GetByIdAsync(transactionId);

                if (unplannedTransaction != null)
                    await _unplannedTransactionsRepository.DeleteAsync(unplannedTransaction);
                else if (plannedTransaction != null)
                    await _plannedTransactionsRepository.DeleteAsync(plannedTransaction);
                else
                    throw new Exception("Transação não encontrada!");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
    }
}