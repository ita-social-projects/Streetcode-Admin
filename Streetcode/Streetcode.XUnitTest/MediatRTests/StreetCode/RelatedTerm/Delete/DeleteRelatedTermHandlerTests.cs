﻿using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.BLL.MediatR.Streetcode.RelatedTerm.Delete;
using Streetcode.DAL.Repositories.Interfaces.Base;
using System.Linq.Expressions;
using Xunit;
using Entity = Streetcode.DAL.Entities.Streetcode.TextContent.RelatedTerm;
using EntityDTO = Streetcode.BLL.DTO.Streetcode.TextContent.RelatedTermDTO;

namespace Streetcode.XUnitTest.MediatRTests.StreetCode.RelatedTerm.Delete
{
    public class DeleteRelatedTermHandlerTests
    {
        private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILoggerService> _mockLogger;

        public DeleteRelatedTermHandlerTests()
        {
            _repositoryWrapperMock = new Mock<IRepositoryWrapper>();
            _mapperMock = new Mock<IMapper>();
            _mockLogger = new Mock<ILoggerService>();
        }

        [Theory]
        [InlineData(1, "test", 1)]
        public async void Handle_WhenRelatedTermExists_DeletesItAndReturnsSuccessResult(int id, string word, int termId)
        {
            // Arrange
            var relatedTerm = CreateNewEntity(id, word, termId);
            _repositoryWrapperMock.Setup(r => r.RelatedTermRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Entity, bool>>>(),
                It.IsAny<Func<IQueryable<Entity>, IIncludableQueryable<Entity, object>>>())).ReturnsAsync(relatedTerm);
            _repositoryWrapperMock.Setup(r => r.RelatedTermRepository.Delete(relatedTerm));
            _repositoryWrapperMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
            var handler = new DeleteRelatedTermHandler(_repositoryWrapperMock.Object, _mapperMock.Object, _mockLogger.Object);
            var command = new DeleteRelatedTermCommand(word);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
            _repositoryWrapperMock.Verify(r => r.RelatedTermRepository.Delete(relatedTerm), Times.Once);
            _repositoryWrapperMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            Assert.Multiple(
            () => Assert.NotNull(result));
        }

        [Theory]
        [InlineData("word")]
        public async void Handle_WhenRelatedTermDoesNotExist_ReturnsFailedResult(string word)
        {

            // Arrange
            _repositoryWrapperMock.Setup(r => r.RelatedTermRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Entity, bool>>>(),
                 It.IsAny<Func<IQueryable<Entity>, IIncludableQueryable<Entity, object>>>())).ReturnsAsync((Entity)null);
            var sut = new DeleteRelatedTermHandler(_repositoryWrapperMock.Object, _mapperMock.Object, _mockLogger.Object);
            var command = new DeleteRelatedTermCommand(word);

            // Act
            var result = await sut.Handle(command, CancellationToken.None);

            // Assert
            Assert.Multiple(
            () => Assert.IsType<Result<EntityDTO>>(result),
            () => Assert.False(result.IsSuccess),
            () => Assert.Equal($"Cannot find a related term: {word}",
            result.Errors.First().Message)
                );
        }

        [Theory]
        [InlineData(1, "Test Word", 1)]
        public async void Handle_WhenDeletionFails_ReturnsFailedResult(int id, string word, int termId)
        {
            // Arrange

            var relatedTerm = CreateNewEntity(id, word, termId);
            _repositoryWrapperMock.Setup(r => r.RelatedTermRepository.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Entity, bool>>>(),
               It.IsAny<Func<IQueryable<Entity>, IIncludableQueryable<Entity, object>>>())).ReturnsAsync(relatedTerm);
            _repositoryWrapperMock.Setup(rw => rw.RelatedTermRepository.Delete(It.IsAny<Entity>()));
            _repositoryWrapperMock.Setup(rw => rw.SaveChangesAsync())
                 .ReturnsAsync(0);
            var sut = new DeleteRelatedTermHandler(_repositoryWrapperMock.Object, _mapperMock.Object, _mockLogger.Object);
            var command = new DeleteRelatedTermCommand(word);
            var expectedError = "Failed to delete a related term";

            // Act
            var result = await sut.Handle(command, CancellationToken.None);

            // Assert
            Assert.Multiple(
          () => Assert.IsType<Result<EntityDTO>>(result),
          () => Assert.False(result.IsSuccess),
          () => Assert.Equal(expectedError, result.Errors.First().Message)
                );
        }

        private static Entity CreateNewEntity(int id, string word, int termId)
        {
            return new Entity { Id = id, Word = word, TermId = termId };
        }
    }
}
