using FinancialSystem.Application.Services.EnvironmentServices;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Enums;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialSystem.Test.EnvironmentTests
{
    public class TransactionServiceTests
    {
        [Fact]
        public async Task ShouldAddPlannedTransactionSuccessfully()
        {
            // Arrange
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var service = new TransactionAppService(mockRepo1.Object, mockRepo2.Object);

            var dto = new TransactionDto
            {
                Type = FinancialRecordTypeEnum.Expense,
                Amount = 1000,
                Description = "Gasto do Cartão",
                RecurrenceType = RecurrenceTypeEnum.None,
                TransactionDate = new DateTime(30 / 01 / 2012)
            };

            // Act
            var exception = await Record.ExceptionAsync(() => service.InsertPlannedTransaction(dto));

            // Assert
            Assert.Null(exception);
            mockRepo1.Verify(r => r.InsertAsync(It.IsAny<PlannedExpensesAndProfits>()), Times.Once);
        }

        [Fact]
        public async Task ShouldAddUnplannedTransactionSuccessfully()
        {
            // Arrange
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var service = new TransactionAppService(mockRepo1.Object, mockRepo2.Object);

            var dto = new TransactionDto
            {
                Type = FinancialRecordTypeEnum.Expense,
                Amount = 1000,
                Description = "Gasto mecânico",
                RecurrenceType = RecurrenceTypeEnum.None,
                TransactionDate = new DateTime(30 / 01 / 2012)
            };

            // Act
            var exception = await Record.ExceptionAsync(() => service.InsertUnplannedTransaction(dto));

            // Assert
            Assert.Null(exception);
            mockRepo2.Verify(r => r.InsertAsync(It.IsAny<UnplannedExpensesAndProfits>()), Times.Once);
        }
    }
}