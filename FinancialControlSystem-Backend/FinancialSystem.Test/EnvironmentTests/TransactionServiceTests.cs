using FinancialSystem.Application.Services.EnvironmentServices;
using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Enums;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System.Linq.Expressions;

namespace FinancialSystem.Test.EnvironmentTests
{
    public class AsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public AsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        {
        }

        public AsyncEnumerable(Expression expression)
            : base(expression)
        {
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider
            => new AsyncQueryProvider<T>(this);
    }

    public class AsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public AsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
            => new ValueTask<bool>(_inner.MoveNext());
    }

    public class AsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public AsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
            => new AsyncEnumerable<TEntity>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new AsyncEnumerable<TElement>(expression);

        public object Execute(Expression expression)
            => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression)
            => _inner.Execute<TResult>(expression);

        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
            => new AsyncEnumerable<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
            => Execute<TResult>(expression);
    }

    public class TransactionServiceTests
    {
        #region InsertionTestsForPlanned
        [Fact]
        public async Task ShouldAddPlannedTransactionSuccessfully()
        {
            // Arrange
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.Setup(x => x.EnvironmentId)
                          .Returns(Guid.Parse("877e85aa-2ada-4e12-b0bb-b4eb9a6be61c"));

            var service = new TransactionAppService(mockAppSession.Object,
                                                    mockRepo1.Object, mockRepo2.Object,
                                                    mockRepo3.Object);

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
        public async Task ShouldExceptionWhenRepositoryFails()
        {
            // Arrange
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockRepo1.Setup(r => r.InsertAsync(It.IsAny<PlannedExpensesAndProfits>()))
                     .ThrowsAsync(new Exception("Erro no banco"));

            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.EnvironmentId).Returns(Guid.NewGuid());

            var service = new TransactionAppService(
                mockAppSession.Object, mockRepo1.Object, mockRepo2.Object, mockRepo3.Object
            );

            var dto = new TransactionDataDto
            {
                Type = (int)FinancialRecordTypeEnum.Profit,
                Amount = 500,
                Description = "Salário",
                RecurrenceType = (int)RecurrenceTypeEnum.Monthly,
                TransactionDate = DateTime.Now
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.InsertPlannedTransaction(dto));
        }

        [Fact]
        public async Task ShouldSanitizeDescriptionBeforeInsert()
        {
            // Arrange
            var capturedEntity = (PlannedExpensesAndProfits)null;

            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockRepo1.Setup(r => r.InsertAsync(It.IsAny<PlannedExpensesAndProfits>()))
                     .Callback<PlannedExpensesAndProfits>(e => capturedEntity = e)
                     .Returns(Task.CompletedTask);

            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.EnvironmentId).Returns(Guid.NewGuid());

            var service = new TransactionAppService(
                mockAppSession.Object, mockRepo1.Object, mockRepo2.Object, mockRepo3.Object
            );

            var dto = new TransactionDataDto
            {
                Type = 1,
                Amount = 10,
                Description = "   Teste   descrição   ",
                RecurrenceType = 0,
                TransactionDate = new DateTime(30 / 01 / 2012)
            };

            // Act
            await service.InsertPlannedTransaction(dto);

            // Assert
            Assert.NotNull(capturedEntity);
        }

        [Fact]
        public async Task ShouldConvertDateToUtcBasedOnBrasiliaTimezone()
        {
            // Arrange
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            PlannedExpensesAndProfits savedEntity = null;
            mockRepo1.Setup(r => r.InsertAsync(It.IsAny<PlannedExpensesAndProfits>()))
                     .Callback<PlannedExpensesAndProfits>(e => savedEntity = e)
                     .Returns(Task.CompletedTask);

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.EnvironmentId).Returns(Guid.NewGuid());

            var service = new TransactionAppService(
                mockAppSession.Object, mockRepo1.Object, mockRepo2.Object, mockRepo3.Object
            );

            var dto = new TransactionDataDto
            {
                Type = 1,
                Amount = 10,
                Description = "Teste",
                RecurrenceType = 0,
                TransactionDate = new DateTime(2024, 01, 01, 15, 0, 0) // 15h horário de Brasília
            };

            // Act
            await service.InsertPlannedTransaction(dto);

            // Assert
            Assert.NotNull(savedEntity);
            var expectedUtc = TimeZoneInfo.ConvertTimeToUtc(dto.TransactionDate,
                TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));

            Assert.Equal(expectedUtc, savedEntity.TransactionDate);
        }
        #endregion

        #region InsertionTestsForUnplanned
        [Fact]
        public async Task ShouldAddUnplannedTransactionSuccessfully()
        {
            // Arrange
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.Setup(x => x.EnvironmentId)
                          .Returns(Guid.Parse("877e85aa-2ada-4e12-b0bb-b4eb9a6be61c"));

            var service = new TransactionAppService(mockAppSession.Object,
                                                    mockRepo1.Object, mockRepo2.Object,
                                                    mockRepo3.Object);

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
        public async Task ShouldExceptionWhenRepositoryFailsOnInsertUnplanned()
        {
            // Arrange
            var mockRepoPlanned = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepoUnplanned = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockRepoUnplanned
                .Setup(r => r.InsertAsync(It.IsAny<UnplannedExpensesAndProfits>()))
                .ThrowsAsync(new Exception("Erro no banco"));

            var mockRepoEnv = new Mock<IGeneralRepository<Environments>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.EnvironmentId).Returns(Guid.NewGuid());

            var service = new TransactionAppService(
                mockAppSession.Object,
                mockRepoPlanned.Object,
                mockRepoUnplanned.Object,
                mockRepoEnv.Object
            );

            var dto = new TransactionDataDto
            {
                Type = (int)FinancialRecordTypeEnum.Profit,
                Amount = 500,
                Description = "Venda inesperada",
                RecurrenceType = (int)RecurrenceTypeEnum.None,
                TransactionDate = DateTime.Now
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.InsertUnplannedTransaction(dto));
        }

        [Fact]
        public async Task ShouldInsertUnplannedTransactionWithCorrectMappedValues()
        {
            // Arrange
            var mockRepoPlanned = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepoUnplanned = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepoEnv = new Mock<IGeneralRepository<Environments>>();

            UnplannedExpensesAndProfits? capturedTransaction = null;

            mockRepoUnplanned
                .Setup(r => r.InsertAsync(It.IsAny<UnplannedExpensesAndProfits>()))
                .Callback<UnplannedExpensesAndProfits>(t => capturedTransaction = t)
                .Returns(Task.CompletedTask);

            var environmentId = Guid.NewGuid();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.EnvironmentId).Returns(environmentId);

            var service = new TransactionAppService(
                mockAppSession.Object,
                mockRepoPlanned.Object,
                mockRepoUnplanned.Object,
                mockRepoEnv.Object
            );

            var dto = new TransactionDataDto
            {
                Type = (int)FinancialRecordTypeEnum.Profit,
                Amount = 999.99,
                Description = "Receita surpresa",
                TransactionDate = new DateTime(2024, 12, 1),
                RecurrenceType = (int)RecurrenceTypeEnum.None
            };

            // Act
            await service.InsertUnplannedTransaction(dto);

            // Assert
            Assert.NotNull(capturedTransaction);
            Assert.Equal(dto.Amount, capturedTransaction.Amount);
            Assert.Equal("Receita surpresa", capturedTransaction.Description);
            Assert.Equal(environmentId, capturedTransaction.EnvironmentId);
            Assert.Equal(FinancialRecordTypeEnum.Profit, capturedTransaction.Type);
            Assert.NotEqual(Guid.Empty, capturedTransaction.Id);
        }
        #endregion

        #region UpdateTestsForPlanned
        [Fact]
        public async Task ShouldUpdatePlannedTransactionSuccessfully()
        {
            // Arrange
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new TransactionAppService(mockAppSession.Object,
                                                    mockRepo1.Object, mockRepo2.Object,
                                                    mockRepo3.Object);

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
        public async Task ShouldThrowException_WhenIdIsNull()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var service = new TransactionAppService(
                Mock.Of<IAppSession>(), mockRepo1.Object, mockRepo2.Object, mockRepo3.Object
            );

            var dto = new TransactionDataDto { Id = null };

            // Act
            var ex = await Record.ExceptionAsync(() => service.UpdatePlannedTransaction(dto));

            // Assert
            Assert.NotNull(ex);
        }

        [Fact]
        public async Task ShouldThrowException_WhenIdIsEmpty()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var service = new TransactionAppService(
                Mock.Of<IAppSession>(), mockRepo1.Object, mockRepo2.Object, mockRepo3.Object
            );

            var dto = new TransactionDataDto { Id = Guid.Empty };

            var ex = await Record.ExceptionAsync(() => service.UpdatePlannedTransaction(dto));

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task ShouldThrowException_WhenTransactionNotFound()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            mockRepo1.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<PlannedExpensesAndProfits, bool>>>()))
                .ReturnsAsync((PlannedExpensesAndProfits)null);

            var service = new TransactionAppService(
                Mock.Of<IAppSession>(), mockRepo1.Object, mockRepo2.Object, mockRepo3.Object
            );

            var dto = new TransactionDataDto { Id = Guid.NewGuid() };

            var ex = await Record.ExceptionAsync(() => service.UpdatePlannedTransaction(dto));

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task ShouldThrowException_WhenRepositoryUpdateFails()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            mockRepo1.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<PlannedExpensesAndProfits, bool>>>()))
                     .ReturnsAsync(new PlannedExpensesAndProfits { Id = Guid.NewGuid() });

            mockRepo1.Setup(r => r.UpdateAsync(It.IsAny<PlannedExpensesAndProfits>()))
                     .ThrowsAsync(new Exception("DB error"));

            var service = new TransactionAppService(
                Mock.Of<IAppSession>(), mockRepo1.Object, mockRepo2.Object, mockRepo3.Object
            );

            var dto = new TransactionDataDto { Id = Guid.NewGuid(), Amount = 10, Type = 1, Description = "teste" };

            // Act
            var ex = await Record.ExceptionAsync(() => service.UpdatePlannedTransaction(dto));

            // Assert
            Assert.NotNull(ex);
        }
        #endregion

        #region UpdateTestsForUnplanned
        [Fact]
        public async Task ShouldUpdateUnplannedTransactionSuccessfully()
        {
            // Arrange
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new TransactionAppService(mockAppSession.Object,
                                                    mockRepo1.Object, mockRepo2.Object,
                                                    mockRepo3.Object);

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
        public async Task ShouldThrowExceptionWhenIdIsNull()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var service = new TransactionAppService(
                Mock.Of<IAppSession>(), mockRepo1.Object, mockRepo2.Object, mockRepo3.Object
            );

            var dto = new TransactionDataDto { Id = null };

            // Act
            var ex = await Record.ExceptionAsync(() => service.UpdateUnplannedTransaction(dto));

            // Assert
            Assert.NotNull(ex);
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenIdIsEmpty()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var service = new TransactionAppService(
                Mock.Of<IAppSession>(), mockRepo1.Object, mockRepo2.Object, mockRepo3.Object
            );

            var dto = new TransactionDataDto { Id = Guid.Empty };

            var ex = await Record.ExceptionAsync(() => service.UpdateUnplannedTransaction(dto));

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task ShouldThrowException_WhenUnplannedTransactionNotFound()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            mockRepo2.Setup(r =>
                r.FirstOrDefaultAsync(It.IsAny<Expression<Func<UnplannedExpensesAndProfits, bool>>>()))
                .ReturnsAsync((UnplannedExpensesAndProfits)null);

            var service = new TransactionAppService(
                Mock.Of<IAppSession>(), mockRepo1.Object, mockRepo2.Object, mockRepo3.Object
            );

            var dto = new TransactionDataDto { Id = Guid.NewGuid() };

            var ex = await Record.ExceptionAsync(() => service.UpdateUnplannedTransaction(dto));

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task ShouldExceptionWhenRepositoryUpdateFails()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            mockRepo2.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<UnplannedExpensesAndProfits, bool>>>()))
                     .ReturnsAsync(new UnplannedExpensesAndProfits { Id = Guid.NewGuid() });

            mockRepo2.Setup(r => r.UpdateAsync(It.IsAny<UnplannedExpensesAndProfits>()))
                     .ThrowsAsync(new Exception("Erro ao atualizar"));

            var service = new TransactionAppService(
                Mock.Of<IAppSession>(), mockRepo1.Object, mockRepo2.Object, mockRepo3.Object
            );

            var dto = new TransactionDataDto
            {
                Id = Guid.NewGuid(),
                Amount = 500,
                Type = 1,
                Description = "Teste Erro"
            };

            var ex = await Record.ExceptionAsync(() => service.UpdateUnplannedTransaction(dto));

            Assert.NotNull(ex);
        }
        #endregion

        #region DeletionTests
        [Fact]
        public async Task ShouldDeletePlannedTransactionSuccessfully()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var service = new TransactionAppService(Mock.Of<IAppSession>(),
                                                    mockRepo1.Object, mockRepo2.Object, mockRepo3.Object);

            var id = Guid.NewGuid();
            mockRepo1.Setup(r => r.GetByIdAsync(id))
                     .ReturnsAsync(new PlannedExpensesAndProfits { Id = id });

            await service.DeleteTransaction(id);

            mockRepo1.Verify(r => r.DeleteAsync(It.IsAny<PlannedExpensesAndProfits>()), Times.Once);
        }

        [Fact]
        public async Task ShouldDeleteUnplannedTransactionSuccessfully()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var service = new TransactionAppService(Mock.Of<IAppSession>(),
                                                    mockRepo1.Object, mockRepo2.Object, mockRepo3.Object);

            var id = Guid.NewGuid();
            mockRepo1.Setup(r => r.GetByIdAsync(id))
                     .ReturnsAsync((PlannedExpensesAndProfits)null);

            mockRepo2.Setup(r => r.GetByIdAsync(id))
                     .ReturnsAsync(new UnplannedExpensesAndProfits { Id = id });

            await service.DeleteTransaction(id);

            mockRepo2.Verify(r => r.DeleteAsync(It.IsAny<UnplannedExpensesAndProfits>()), Times.Once);
        }

        [Fact]
        public async Task ShouldExceptionWhenTransactionNotFound()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var service = new TransactionAppService(Mock.Of<IAppSession>(),
                                                    mockRepo1.Object, mockRepo2.Object, mockRepo3.Object);

            var id = Guid.NewGuid();

            mockRepo1.Setup(r => r.GetByIdAsync(id))
                     .ReturnsAsync((PlannedExpensesAndProfits)null);

            mockRepo2.Setup(r => r.GetByIdAsync(id))
                     .ReturnsAsync((UnplannedExpensesAndProfits)null);

            var ex = await Record.ExceptionAsync(() => service.DeleteTransaction(id));

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task ShouldThrowException_WhenPlannedRepoThrowsOnGet()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            mockRepo1.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                     .ThrowsAsync(new Exception("Erro inesperado"));

            var service = new TransactionAppService(Mock.Of<IAppSession>(),
                                                    mockRepo1.Object, mockRepo2.Object, mockRepo3.Object);

            var id = Guid.NewGuid();

            var ex = await Record.ExceptionAsync(() => service.DeleteTransaction(id));

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task ShouldThrowException_WhenDeletePlannedFails()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var id = Guid.NewGuid();

            mockRepo1.Setup(r => r.GetByIdAsync(id))
                     .ReturnsAsync(new PlannedExpensesAndProfits { Id = id });

            mockRepo1.Setup(r => r.DeleteAsync(It.IsAny<PlannedExpensesAndProfits>()))
                     .ThrowsAsync(new Exception("Erro ao deletar"));

            var service = new TransactionAppService(Mock.Of<IAppSession>(),
                                                    mockRepo1.Object, mockRepo2.Object, mockRepo3.Object);

            var ex = await Record.ExceptionAsync(() => service.DeleteTransaction(id));

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task ShouldThrowException_WhenDeleteUnplannedFails()
        {
            var mockRepo1 = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepo2 = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepo3 = new Mock<IGeneralRepository<Environments>>();

            var id = Guid.NewGuid();

            mockRepo1.Setup(r => r.GetByIdAsync(id))
                     .ReturnsAsync((PlannedExpensesAndProfits)null);

            mockRepo2.Setup(r => r.GetByIdAsync(id))
                     .ReturnsAsync(new UnplannedExpensesAndProfits { Id = id });

            mockRepo2.Setup(r => r.DeleteAsync(It.IsAny<UnplannedExpensesAndProfits>()))
                     .ThrowsAsync(new Exception("Erro ao deletar unplanned"));

            var service = new TransactionAppService(Mock.Of<IAppSession>(),
                                                    mockRepo1.Object, mockRepo2.Object, mockRepo3.Object);

            var ex = await Record.ExceptionAsync(() => service.DeleteTransaction(id));

            Assert.NotNull(ex);
        }
        #endregion

        #region UpdateTotalBalanceTests
        [Fact]
        public async Task ShouldThrowException_WhenEnvironmentNotFound()
        {
            // Arrange
            var mockRepoPlanned = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockRepoUnplanned = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockRepoEnv = new Mock<IGeneralRepository<Environments>>();

            mockRepoEnv.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                       .ReturnsAsync((Environments)null);

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.EnvironmentId).Returns(Guid.NewGuid());

            var service = new TransactionAppService(
                mockAppSession.Object, mockRepoPlanned.Object, mockRepoUnplanned.Object, mockRepoEnv.Object
            );

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.UpdateEnvironmentBalance());
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenEnvironmentNotFound()
        {
            var envId = Guid.NewGuid();

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var mockEnvRepo = new Mock<IGeneralRepository<Environments>>();
            mockEnvRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                       .ReturnsAsync((Environments)null);

            var mockPlannedRepo = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockUnplannedRepo = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();

            var service = new TransactionAppService(
                mockApp.Object, mockPlannedRepo.Object,
                mockUnplannedRepo.Object, mockEnvRepo.Object);

            await Assert.ThrowsAsync<Exception>(() => service.UpdateEnvironmentBalance());
        }

        [Fact]
        public async Task ShouldNotChangeEnvironmentWhenNoTransactions()
        {
            var envId = Guid.NewGuid();

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var environment = new Environments { Id = envId, TotalBalance = 500 };

            var mockEnvRepo = new Mock<IGeneralRepository<Environments>>();
            mockEnvRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                       .ReturnsAsync(environment);

            // PLANNED — lista vazia com suporte a async
            var plannedAsync = new AsyncEnumerable<PlannedExpensesAndProfits>(
                new List<PlannedExpensesAndProfits>()
            );

            var mockPlannedRepo = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlannedRepo.Setup(r => r.GetAll()).Returns(plannedAsync);

            // UNPLANNED — lista vazia com suporte a async
            var unplannedAsync = new AsyncEnumerable<UnplannedExpensesAndProfits>(
                new List<UnplannedExpensesAndProfits>()
            );

            var mockUnplannedRepo = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplannedRepo.Setup(r => r.GetAll()).Returns(unplannedAsync);

            var service = new TransactionAppService(
                mockApp.Object, mockPlannedRepo.Object,
                mockUnplannedRepo.Object, mockEnvRepo.Object);

            await service.UpdateEnvironmentBalance();

            Assert.Equal(500, environment.TotalBalance);
            mockEnvRepo.Verify(r => r.UpdateAsync(It.IsAny<Environments>()), Times.Never);
        }

        [Fact]
        public async Task ShouldProcessUnplannedTransactionWhenNeverProcessedAndDue()
        {
            var envId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var environment = new Environments { Id = envId, TotalBalance = 0 };

            var unplannedTx = new UnplannedExpensesAndProfits
            {
                EnvironmentId = envId,
                Type = FinancialRecordTypeEnum.Profit,
                Amount = 100,
                TransactionDate = today.AddDays(-1),
                LastProcessedDate = null,
                IsDeleted = false
            };

            var mockEnvRepo = new Mock<IGeneralRepository<Environments>>();
            mockEnvRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                       .ReturnsAsync(environment);

            var mockPlannedRepo = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlannedRepo.Setup(r => r.GetAll())
                           .Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(
                               new List<PlannedExpensesAndProfits>()));

            var mockUnplannedRepo = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplannedRepo.Setup(r => r.GetAll())
                             .Returns(new AsyncEnumerable<UnplannedExpensesAndProfits>(
                                 new List<UnplannedExpensesAndProfits> { unplannedTx }));

            var service = new TransactionAppService(
                mockApp.Object, mockPlannedRepo.Object,
                mockUnplannedRepo.Object, mockEnvRepo.Object);

            await service.UpdateEnvironmentBalance();

            Assert.Equal(100, environment.TotalBalance);
            Assert.Equal(today, unplannedTx.LastProcessedDate);
            mockUnplannedRepo.Verify(r => r.UpdateAsync(unplannedTx), Times.Once);
        }

        [Fact]
        public async Task ShouldProcessPlannedWithoutRecurrence()
        {
            var envId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var environment = new Environments { Id = envId, TotalBalance = 0 };

            var planned = new PlannedExpensesAndProfits
            {
                EnvironmentId = envId,
                Type = FinancialRecordTypeEnum.Expense,
                RecurrenceType = RecurrenceTypeEnum.None,
                Amount = 200,
                TransactionDate = today.AddDays(-1),
                LastProcessedDate = null,
                IsDeleted = false
            };

            var mockEnvRepo = new Mock<IGeneralRepository<Environments>>();
            mockEnvRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                       .ReturnsAsync(environment);

            var mockUnplannedRepo = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplannedRepo.Setup(r => r.GetAll())
                             .Returns(new AsyncEnumerable<UnplannedExpensesAndProfits>(
                                 new List<UnplannedExpensesAndProfits>()));

            var mockPlannedRepo = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlannedRepo.Setup(r => r.GetAll())
                           .Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(
                               new List<PlannedExpensesAndProfits> { planned }));

            var service = new TransactionAppService(
                mockApp.Object, mockPlannedRepo.Object,
                mockUnplannedRepo.Object, mockEnvRepo.Object);

            await service.UpdateEnvironmentBalance();

            Assert.Equal(-200, environment.TotalBalance);
            Assert.Equal(today, planned.LastProcessedDate);
            mockPlannedRepo.Verify(r => r.UpdateAsync(planned), Times.Once);
        }

        [Fact]
        public async Task ShouldProcessDailyRecurrence()
        {
            var envId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var environment = new Environments { Id = envId, TotalBalance = 0 };

            var planned = new PlannedExpensesAndProfits
            {
                EnvironmentId = envId,
                Type = FinancialRecordTypeEnum.Profit,
                RecurrenceType = RecurrenceTypeEnum.Daily,
                Amount = 50,
                TransactionDate = today.AddDays(-3),
                LastProcessedDate = today.AddDays(-1),
                IsDeleted = false
            };

            var mockEnvRepo = new Mock<IGeneralRepository<Environments>>();
            mockEnvRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                       .ReturnsAsync(environment);

            var mockUnplannedRepo = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplannedRepo.Setup(r => r.GetAll())
                             .Returns(new AsyncEnumerable<UnplannedExpensesAndProfits>(
                                 new List<UnplannedExpensesAndProfits>()));

            var mockPlannedRepo = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlannedRepo.Setup(r => r.GetAll())
                           .Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(
                               new List<PlannedExpensesAndProfits> { planned }));

            var service = new TransactionAppService(
                mockApp.Object, mockPlannedRepo.Object,
                mockUnplannedRepo.Object, mockEnvRepo.Object);

            await service.UpdateEnvironmentBalance();

            Assert.Equal(50, environment.TotalBalance);
            Assert.Equal(today, planned.LastProcessedDate);
            mockPlannedRepo.Verify(r => r.UpdateAsync(planned), Times.Once);
        }

        [Fact]
        public async Task ShouldProcessWeeklyRecurrence()
        {
            var envId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var environment = new Environments { Id = envId, TotalBalance = 0 };

            var planned = new PlannedExpensesAndProfits
            {
                EnvironmentId = envId,
                Type = FinancialRecordTypeEnum.Profit,
                RecurrenceType = RecurrenceTypeEnum.Weekly,
                Amount = 300,
                TransactionDate = today.AddDays(-20),
                LastProcessedDate = today.AddDays(-10),
                IsDeleted = false
            };

            var mockEnvRepo = new Mock<IGeneralRepository<Environments>>();
            mockEnvRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                       .ReturnsAsync(environment);

            var mockUnplannedRepo = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplannedRepo.Setup(r => r.GetAll())
                             .Returns(new AsyncEnumerable<UnplannedExpensesAndProfits>(
                                 new List<UnplannedExpensesAndProfits>()));

            var mockPlannedRepo = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlannedRepo.Setup(r => r.GetAll())
                           .Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(
                               new List<PlannedExpensesAndProfits> { planned }));

            var service = new TransactionAppService(
                mockApp.Object, mockPlannedRepo.Object,
                mockUnplannedRepo.Object, mockEnvRepo.Object);

            await service.UpdateEnvironmentBalance();

            Assert.Equal(300, environment.TotalBalance);
            Assert.Equal(today, planned.LastProcessedDate);
            mockPlannedRepo.Verify(r => r.UpdateAsync(planned), Times.Once);
        }

        [Fact]
        public async Task ShouldProcessMonthlyRecurrence()
        {
            var envId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var environment = new Environments { Id = envId, TotalBalance = 0 };

            var planned = new PlannedExpensesAndProfits
            {
                EnvironmentId = envId,
                Type = FinancialRecordTypeEnum.Expense,
                RecurrenceType = RecurrenceTypeEnum.Monthly,
                Amount = 80,
                TransactionDate = today.AddMonths(-3),
                LastProcessedDate = today.AddMonths(-2),
                IsDeleted = false
            };

            var mockEnvRepo = new Mock<IGeneralRepository<Environments>>();
            mockEnvRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                       .ReturnsAsync(environment);

            var mockUnplannedRepo = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplannedRepo.Setup(r => r.GetAll())
                             .Returns(new AsyncEnumerable<UnplannedExpensesAndProfits>(
                                 new List<UnplannedExpensesAndProfits>()));

            var mockPlannedRepo = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlannedRepo.Setup(r => r.GetAll())
                           .Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(
                               new List<PlannedExpensesAndProfits> { planned }));

            var service = new TransactionAppService(
                mockApp.Object, mockPlannedRepo.Object,
                mockUnplannedRepo.Object, mockEnvRepo.Object);

            await service.UpdateEnvironmentBalance();

            Assert.Equal(-80, environment.TotalBalance);
            Assert.Equal(today, planned.LastProcessedDate);
            mockPlannedRepo.Verify(r => r.UpdateAsync(planned), Times.Once);
        }
        #endregion

        #region GetTransactionsTests
        [Fact]
        public async Task GetAllPlannedTransactions_ShouldReturnEmptyList_WhenNoItemsFound()
        {
            var envId = Guid.NewGuid();

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var repoList = new List<PlannedExpensesAndProfits>();
            var asyncList = new AsyncEnumerable<PlannedExpensesAndProfits>(repoList);

            var mockPlannedRepo = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlannedRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var mockUnplannedRepo = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            var mockEnvRepo = new Mock<IGeneralRepository<Environments>>();

            var service = new TransactionAppService(
                mockApp.Object, mockPlannedRepo.Object,
                mockUnplannedRepo.Object, mockEnvRepo.Object);

            var result = await service.GetAllPlannedTransactions();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllPlannedTransactions_ShouldFilterByEnvironmentId()
        {
            var envId = Guid.NewGuid();

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var repoList = new List<PlannedExpensesAndProfits>
            {
                new PlannedExpensesAndProfits { Id = Guid.NewGuid(), Amount = 100, Description="A", EnvironmentId = envId, IsDeleted = false },
                new PlannedExpensesAndProfits { Id = Guid.NewGuid(), Amount = 999, Description="B", EnvironmentId = Guid.NewGuid(), IsDeleted = false }
            };

            var asyncList = new AsyncEnumerable<PlannedExpensesAndProfits>(repoList);

            var mockPlannedRepo = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlannedRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var service = new TransactionAppService(
                mockApp.Object, mockPlannedRepo.Object, null, null);

            var result = await service.GetAllPlannedTransactions();

            Assert.Single(result);
            Assert.Equal(100, result.First().Amount);
        }

        [Fact]
        public async Task GetAllPlannedTransactions_ShouldIgnoreDeletedItems()
        {
            var envId = Guid.NewGuid();

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var repoList = new List<PlannedExpensesAndProfits>
            {
                new PlannedExpensesAndProfits { Id = Guid.NewGuid(), Amount = 200, Description="Valid", EnvironmentId = envId, IsDeleted = false },
                new PlannedExpensesAndProfits { Id = Guid.NewGuid(), Amount = 300, Description="Deleted", EnvironmentId = envId, IsDeleted = true }
            };

            var asyncList = new AsyncEnumerable<PlannedExpensesAndProfits>(repoList);

            var mockPlannedRepo = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlannedRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var service = new TransactionAppService(
                mockApp.Object, mockPlannedRepo.Object, null, null);

            var result = await service.GetAllPlannedTransactions();

            Assert.Single(result);
            Assert.Equal(200, result.First().Amount);
        }

        [Fact]
        public async Task GetAllPlannedTransactions_ShouldMapToDtoCorrectly()
        {
            var envId = Guid.NewGuid();
            var tranId = Guid.NewGuid();

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var repoList = new List<PlannedExpensesAndProfits>
            {
                new PlannedExpensesAndProfits
                {
                    Id = tranId,
                    Amount = 50,
                    Description = "Teste",
                    Type = FinancialRecordTypeEnum.Expense,
                    RecurrenceType = RecurrenceTypeEnum.Monthly,
                    TransactionDate = DateTime.Today,
                    EnvironmentId = envId,
                    IsDeleted = false
                }
            };

            var asyncList = new AsyncEnumerable<PlannedExpensesAndProfits>(repoList);

            var mockPlannedRepo = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlannedRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var service = new TransactionAppService(
                mockApp.Object, mockPlannedRepo.Object, null, null);

            var result = await service.GetAllPlannedTransactions();

            var item = result.First();

            Assert.Equal(tranId, item.Id);
            Assert.Equal(50, item.Amount);
            Assert.Equal("Teste", item.Description);
            Assert.Equal((int)FinancialRecordTypeEnum.Expense, item.Type);
            Assert.Equal((int)RecurrenceTypeEnum.Monthly, item.RecurrenceType);
        }

        [Fact]
        public async Task GetAllUnplannedTransactions_ShouldReturnEmptyList_WhenNoItemsFound()
        {
            var envId = Guid.NewGuid();

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var asyncList = new AsyncEnumerable<UnplannedExpensesAndProfits>(
                new List<UnplannedExpensesAndProfits>()
            );

            var mockUnplannedRepo = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplannedRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var service = new TransactionAppService(
                mockApp.Object, null, mockUnplannedRepo.Object, null);

            var result = await service.GetAllUnplannedTransactions();

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllUnplannedTransactions_ShouldFilterByEnvironmentId()
        {
            var envId = Guid.NewGuid();

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var repoList = new List<UnplannedExpensesAndProfits>
            {
                new UnplannedExpensesAndProfits { Id = Guid.NewGuid(), Amount = 10, Description="A", EnvironmentId = envId },
                new UnplannedExpensesAndProfits { Id = Guid.NewGuid(), Amount = 20, Description="B", EnvironmentId = Guid.NewGuid() }
            };

            var asyncList = new AsyncEnumerable<UnplannedExpensesAndProfits>(repoList);

            var mockUnplannedRepo = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplannedRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var service = new TransactionAppService(
                mockApp.Object, null, mockUnplannedRepo.Object, null);

            var result = await service.GetAllUnplannedTransactions();

            Assert.Single(result);
            Assert.Equal(10, result.First().Amount);
        }

        [Fact]
        public async Task GetAllUnplannedTransactions_ShouldIgnoreDeletedItems()
        {
            var envId = Guid.NewGuid();

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var repoList = new List<UnplannedExpensesAndProfits>
            {
                new UnplannedExpensesAndProfits { Id = Guid.NewGuid(), Amount = 50, EnvironmentId = envId, IsDeleted = false },
                new UnplannedExpensesAndProfits { Id = Guid.NewGuid(), Amount = 99, EnvironmentId = envId, IsDeleted = true }
            };

            var asyncList = new AsyncEnumerable<UnplannedExpensesAndProfits>(repoList);

            var mockUnplannedRepo = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplannedRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var service = new TransactionAppService(
                mockApp.Object, null, mockUnplannedRepo.Object, null);

            var result = await service.GetAllUnplannedTransactions();

            Assert.Single(result);
            Assert.Equal(50, result.First().Amount);
        }

        [Fact]
        public async Task GetAllUnplannedTransactions_ShouldMapToDtoCorrectly()
        {
            var envId = Guid.NewGuid();
            var tranId = Guid.NewGuid();

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var repoList = new List<UnplannedExpensesAndProfits>
            {
                new UnplannedExpensesAndProfits
                {
                    Id = tranId,
                    Amount = 75,
                    Description = "XYZ",
                    Type = FinancialRecordTypeEnum.Profit,
                    TransactionDate = DateTime.Today,
                    EnvironmentId = envId,
                    IsDeleted = false
                }
            };

            var asyncList = new AsyncEnumerable<UnplannedExpensesAndProfits>(repoList);

            var mockUnplannedRepo = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplannedRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var service = new TransactionAppService(
                mockApp.Object, null, mockUnplannedRepo.Object, null);

            var result = await service.GetAllUnplannedTransactions();

            var item = result.First();

            Assert.Equal(tranId, item.Id);
            Assert.Equal(75, item.Amount);
            Assert.Equal("XYZ", item.Description);
            Assert.Equal((int)FinancialRecordTypeEnum.Profit, item.Type);
        }
        #endregion
    }
}