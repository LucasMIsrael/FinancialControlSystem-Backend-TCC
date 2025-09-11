using FinancialSystem.Application.Services.EnvironmentServices;
using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Enums;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Moq;
using System.Linq.Expressions;

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

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.Setup(x => x.EnvironmentId)
                          .Returns(Guid.Parse("877e85aa-2ada-4e12-b0bb-b4eb9a6be61c"));

            var service = new TransactionAppService(mockAppSession.Object, mockRepo1.Object, mockRepo2.Object);

            var dto = new TransactionDataDto
            {
                Type = (int)FinancialRecordTypeEnum.Expense,
                Amount = 1000,
                Description = "Gasto do Cartão",
                RecurrenceType = (int)RecurrenceTypeEnum.None,
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

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.Setup(x => x.EnvironmentId)
                          .Returns(Guid.Parse("877e85aa-2ada-4e12-b0bb-b4eb9a6be61c"));

            var service = new TransactionAppService(mockAppSession.Object, mockRepo1.Object, mockRepo2.Object);

            var dto = new TransactionDataDto
            {
                Type = (int)FinancialRecordTypeEnum.Expense,
                Amount = 1000,
                Description = "Gasto mecânico",
                RecurrenceType = (int)RecurrenceTypeEnum.None,
                TransactionDate = new DateTime(30 / 01 / 2012)
            };

            // Act
            var exception = await Record.ExceptionAsync(() => service.InsertUnplannedTransaction(dto));

            // Assert
            Assert.Null(exception);
            mockRepo2.Verify(r => r.InsertAsync(It.IsAny<UnplannedExpensesAndProfits>()), Times.Once);
        }

        [Fact]
        public async Task ShouldUpdatePlannedTransactionSuccessfully()
        {
            // Arrange
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new TransactionAppService(mockAppSession.Object, mockRepo1.Object, mockRepo2.Object);

            var dto = new TransactionDataDto
            {
                Id = Guid.NewGuid(),
                Type = (int)FinancialRecordTypeEnum.Expense,
                Amount = 1500,
                Description = "Atualizado",
                RecurrenceType = (int)RecurrenceTypeEnum.None,
                TransactionDate = new DateTime(2025, 09, 07)
            };

            mockRepo1.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<PlannedExpensesAndProfits, bool>>>()))
                     .ReturnsAsync(new PlannedExpensesAndProfits { Id = (Guid)dto.Id });

            // Act
            await service.UpdatePlannedTransaction(dto);

            // Assert
            mockRepo1.Verify(r => r.UpdateAsync(It.IsAny<PlannedExpensesAndProfits>()), Times.Once);
        }

        [Fact]
        public async Task ShouldUpdateUnplannedTransactionSuccessfully()
        {
            // Arrange
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new TransactionAppService(mockAppSession.Object, mockRepo1.Object, mockRepo2.Object);

            var dto = new TransactionDataDto
            {
                Id = Guid.NewGuid(),
                Type = (int)FinancialRecordTypeEnum.Expense,
                Amount = 2000,
                Description = "Atualizado",
                RecurrenceType = (int)RecurrenceTypeEnum.None,
                TransactionDate = new DateTime(2025, 09, 07)
            };

            mockRepo2.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<UnplannedExpensesAndProfits, bool>>>()))
                     .ReturnsAsync(new UnplannedExpensesAndProfits { Id = (Guid)dto.Id });

            // Act
            await service.UpdateUnplannedTransaction(dto);

            // Assert
            mockRepo2.Verify(r => r.UpdateAsync(It.IsAny<UnplannedExpensesAndProfits>()), Times.Once);
        }

        [Fact]
        public async Task ShouldReturnPlannedTransactionsSuccessfully()
        {
            // Arrange
            //    var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            //    var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            //    var service = new TransactionAppService(mockRepo1.Object, mockRepo2.Object);

            //    var list = new List<PlannedExpensesAndProfits>
            //{
            //    new PlannedExpensesAndProfits
            //    {
            //        Id = Guid.NewGuid(),
            //        Amount = 100,
            //        Description = "Teste",
            //        RecurrenceType = RecurrenceTypeEnum.None,
            //        TransactionDate = DateTime.UtcNow,
            //        Type = FinancialRecordTypeEnum.Expense
            //    }
            //};

            //    mockRepo1.Setup(r => r.GetAll()).Returns(list.AsQueryable().BuildMock().Object);

            //    // Act
            //    var result = await service.GetAllPlannedTransactions();

            //    // Assert
            //    Assert.Single(result);
            //    Assert.Equal("Teste", result.First().Description);
        }

        [Fact]
        public async Task ShouldReturnUnplannedTransactionsSuccessfully()
        {
            // Arrange
            //var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            //var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            //var service = new TransactionAppService(mockRepo1.Object, mockRepo2.Object);

            //var list = new List<UnplannedExpensesAndProfits>
            //{
            //    new UnplannedExpensesAndProfits
            //    {
            //        Id = Guid.NewGuid(),
            //        Amount = 250,
            //        Description = "Imprevisto",
            //        TransactionDate = DateTime.UtcNow,
            //        Type = FinancialRecordTypeEnum.Expense
            //    }
            //};

            //mockRepo2.Setup(r => r.GetAll()).Returns(list.AsQueryable().BuildMock().Object);

            //// Act
            //var result = await service.GetAllUnplannedTransactions();

            //// Assert
            //Assert.Single(result);
            //Assert.Equal("Imprevisto", result.First().Description);
        }

        [Fact]
        public async Task ShouldDeletePlannedTransactionSuccessfully()
        {
            // Arrange
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new TransactionAppService(mockAppSession.Object, mockRepo1.Object, mockRepo2.Object);

            var id = Guid.NewGuid();
            mockRepo1.Setup(r => r.GetByIdAsync(id))
                     .ReturnsAsync(new PlannedExpensesAndProfits { Id = id });

            // Act
            await service.DeleteTransaction(id);

            // Assert
            mockRepo1.Verify(r => r.DeleteAsync(It.IsAny<PlannedExpensesAndProfits>()), Times.Once);
        }

        [Fact]
        public async Task ShouldDeleteUnplannedTransactionSuccessfully()
        {
            // Arrange
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new TransactionAppService(mockAppSession.Object, mockRepo1.Object, mockRepo2.Object);

            var id = Guid.NewGuid();
            mockRepo1.Setup(r => r.GetByIdAsync(id))
                     .ReturnsAsync((PlannedExpensesAndProfits)null);

            mockRepo2.Setup(r => r.GetByIdAsync(id))
                     .ReturnsAsync(new UnplannedExpensesAndProfits { Id = id });

            // Act
            await service.DeleteTransaction(id);

            // Assert
            mockRepo2.Verify(r => r.DeleteAsync(It.IsAny<UnplannedExpensesAndProfits>()), Times.Once);
        }
    }
}