using Moq;

namespace MockTesting.Tests;

[TestFixture]
public class TransactionProcessorTests
{
    private Mock<IAccountService> _accountServiceMock;
    private Mock<ILogger> _loggerMock;
    private Mock<ITransactionService> _transactionServiceMock;
    private Mock<IPermissionService> _permissionServiceMock;
    private TransactionProcessor _transactionProcessor;



    [SetUp]
    public void Setup()
    {
        this._accountServiceMock = new Mock<IAccountService>();

        this._loggerMock = new Mock<ILogger>();

        this._permissionServiceMock = new Mock<IPermissionService>();

        this._transactionServiceMock = new Mock<ITransactionService>();

        this._transactionProcessor = new TransactionProcessor
        (
            this._permissionServiceMock.Object,
            this._accountServiceMock.Object,
            this._transactionServiceMock.Object,
            this._loggerMock.Object
        );

    }

    [Test]
    public void ProcessTransfer_SuccessfulTransfer_LogsAndReturnsTrue()
    {
        // Arrange
        int fromUserId = 1;

        int toUserId = 2;

        decimal amount = 100;

        _ = this._permissionServiceMock
            .Setup(x => x.HasTransferPermission(fromUserId))
            .Returns(true);

        _ = this._accountServiceMock
            .Setup(a => a.GetBalance(fromUserId))
            .Returns(500);

        // Act

        bool result = this._transactionProcessor.ProcessTransfer(fromUserId, toUserId, amount);

        // Assert

        Assert.That(result, Is.True);

        this._permissionServiceMock.Verify(
        x => x.HasTransferPermission(fromUserId),
        Times.Once);

        this._accountServiceMock.Verify(
        x => x.GetBalance(fromUserId),
        Times.Once);

        this._transactionServiceMock.Verify(
        x => x.Transfer(fromUserId, toUserId, amount),
        Times.Once);

        this._loggerMock.Verify(
        x => x.Log(It.Is<string>(message =>
        message.Contains("started") &&
        message.Contains("Transfer 100 from 1 to 2"))),
        Times.Once);

        this._loggerMock.Verify(
        x => x.Log(It.Is<string>(message =>
        message.Contains("completed successfully"))),
        Times.Once);
    }

    [Test]
    public void ProcessTransfer_NoPermission_LogsAndReturnsFalse()
    {
        // Arrange
        int fromUserId = 1;

        int toUserId = 2;

        decimal amount = 100;

        _ = this._permissionServiceMock
            .Setup(x => x.HasTransferPermission(fromUserId))
            .Returns(false);

        // Act

        bool result = this._transactionProcessor.ProcessTransfer(fromUserId, toUserId, amount);

        //Assert

        Assert.That(result, Is.False);

        this._permissionServiceMock.Verify(
        x => x.HasTransferPermission(fromUserId),
        Times.Once);

        this._transactionServiceMock.Verify(
        x => x.Transfer(fromUserId, toUserId, amount),
        Times.Never);

        this._accountServiceMock.Verify(
        x => x.GetBalance(fromUserId),
        Times.Never);

        this._loggerMock.Verify(
        x => x.Log(It.Is<string>(message =>
        message.Contains("failed: Permission denied"))),
        Times.Once);

    }

    [Test]
    public void ProcessTransfer_InsufficientBalance_LogsAndReturnsFalse()
    {
        // Arrange
        int fromUserId = 1;

        int toUserId = 2;

        decimal amount = 100;

        _ = this._permissionServiceMock
            .Setup(x => x.HasTransferPermission(fromUserId))
            .Returns(true);

        _ = this._accountServiceMock
            .Setup(x => x.GetBalance(fromUserId))
            .Returns(50);

        // Act

        bool result = this._transactionProcessor.ProcessTransfer(fromUserId, toUserId, amount);

        //Assert

        Assert.That(result, Is.False);

        this._permissionServiceMock.Verify(
        x => x.HasTransferPermission(fromUserId),
        Times.Once);

        this._accountServiceMock.Verify(
        x => x.GetBalance(fromUserId),
        Times.Once);

        this._transactionServiceMock.Verify(
        x => x.Transfer(fromUserId, toUserId, amount),
        Times.Never);

        this._loggerMock.Verify(
        x => x.Log(It.Is<string>(
        message => message.Contains("failed: Insufficient funds"))),
        Times.Once);
    }

    [Test]
    public void ProcessTransfer_TransactionThrowsException_LogsAndReturnsFalse()
    {
        // Arrange
        int fromUserId = 1;

        int toUserId = 2;

        decimal amount = 100;

        _ = this._permissionServiceMock
            .Setup(x => x.HasTransferPermission(fromUserId))
            .Returns(true);

        _ = this._accountServiceMock
            .Setup(x => x.GetBalance(fromUserId))
            .Returns(500);

        _ = this._transactionServiceMock
            .Setup(x => x.Transfer(fromUserId, toUserId, amount))
            .Throws(new Exception("Transfer failed"));

        // Act

        bool result = this._transactionProcessor.ProcessTransfer(fromUserId, toUserId, amount);

        //Assert

        Assert.That(result, Is.False);

        this._permissionServiceMock.Verify(
        x => x.HasTransferPermission(fromUserId),
        Times.Once);

        this._accountServiceMock.Verify(
        x => x.GetBalance(fromUserId),
        Times.Once);

        this._transactionServiceMock.Verify(
        x => x.Transfer(fromUserId, toUserId, amount),
        Times.Once);

        this._loggerMock.Verify(
        x => x.Log(It.Is<string>(
        message => message.Contains("failed with error: Transfer failed"))),
        Times.Once);
    }

    [Test]
    public void ProcessTransfer_InvalidFromUserId_ThrowsArgumentException()
    {
        int fromUserId = -1;

        int toUserId = 2;

        decimal amount = 100;

        _ = Assert.Throws<ArgumentException>(() => this._transactionProcessor.ProcessTransfer(fromUserId, toUserId, amount));
    }

    [Test]
    public void ProcessTransfer_InvalidToUserId_ThrowsArgumentException()
    {
        int fromUserId = 1;

        int toUserId = -1;

        decimal amount = 100;

        _ = Assert.Throws<ArgumentException>(() => this._transactionProcessor.ProcessTransfer(fromUserId, toUserId, amount));
    }

    [Test]
    public void ProcessTransfer_SameUserIds_ThrowsArgumentException()
    {
        int fromUserId = 2;

        int toUserId = 2;

        decimal amount = 100;

        _ = Assert.Throws<ArgumentException>(() => this._transactionProcessor.ProcessTransfer(fromUserId, toUserId, amount));
    }

    [Test]
    public void ProcessTransfer_InvalidAmount_ThrowsArgumentException()
    {
        int fromUserId = 1;

        int toUserId = 2;

        decimal amount = 0;

        _ = Assert.Throws<ArgumentException>(() => this._transactionProcessor.ProcessTransfer(fromUserId, toUserId, amount));
    }

    [Test]
    public void Constructor_NullDependencies_ThrowsArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() =>
        new TransactionProcessor(
            null,
            this._accountServiceMock.Object,
            this._transactionServiceMock.Object,
            this._loggerMock.Object
            ));

        _ = Assert.Throws<ArgumentNullException>(() =>
            new TransactionProcessor(
                this._permissionServiceMock.Object,
                null,
                this._transactionServiceMock.Object,
                this._loggerMock.Object));

        _ = Assert.Throws<ArgumentNullException>(() =>
            new TransactionProcessor(
                this._permissionServiceMock.Object,
                this._accountServiceMock.Object,
                null,
                this._loggerMock.Object));

        _ = Assert.Throws<ArgumentNullException>(() =>
            new TransactionProcessor(
                this._permissionServiceMock.Object,
                this._accountServiceMock.Object,
                this._transactionServiceMock.Object,
                null));
    }

    [Test]
    public void ProcessTransfer_WithMaxIntValues_ShouldSucceed()
    {
        // Arrange

        int fromUserId = int.MaxValue;

        int toUserId = int.MaxValue - 1;

        decimal amount = 100;

        _ = this._permissionServiceMock
            .Setup(x => x.HasTransferPermission(fromUserId))
            .Returns(true);

        _ = this._accountServiceMock
            .Setup(x => x.GetBalance(fromUserId))
            .Returns(500);

        // Act

        bool result = this._transactionProcessor.ProcessTransfer(fromUserId, toUserId, amount);

        // Assert

        Assert.That(result, Is.True);

        this._transactionServiceMock.Verify(
        x => x.Transfer(fromUserId, toUserId, amount),
        Times.Once());
    }

    [Test]
    public void ProcessTransfer_WithMinValidValues_ShouldSucceed()
    {
        // Arrange

        int fromUserId = 1;

        int toUserId = 2;

        decimal amount = 1;

        _ = this._permissionServiceMock
            .Setup(x => x.HasTransferPermission(fromUserId))
            .Returns(true);

        _ = this._accountServiceMock
            .Setup(x => x.GetBalance(fromUserId))
            .Returns(500);

        // Act

        bool result = this._transactionProcessor.ProcessTransfer(fromUserId, toUserId, amount);

        // Assert

        Assert.That(result, Is.True);

        this._transactionServiceMock.Verify(
        x => x.Transfer(fromUserId, toUserId, amount),
        Times.Once());
    }

    [Test]
    public void ProcessTransfer_LogMessages_ShouldContainCorrectInformation()
    {
        // Arrange
        int fromUserId = 1;
        int toUserId = 2;
        decimal amount = 100;

        _ = this._permissionServiceMock
            .Setup(x => x.HasTransferPermission(fromUserId))
            .Returns(true);

        _ = this._accountServiceMock
            .Setup(x => x.GetBalance(fromUserId))
            .Returns(50);

        // Act
        bool result = this._transactionProcessor.ProcessTransfer(
            fromUserId,
            toUserId,
            amount);

        // Assert
        Assert.That(result, Is.False);

        this._loggerMock.Verify(
            x => x.Log(It.Is<string>(message =>
                message.Contains("started") &&
                message.Contains("Transfer 100") &&
                message.Contains("from 1") &&
                message.Contains("to 2"))),
            Times.Once);

        this._loggerMock.Verify(
            x => x.Log(It.Is<string>(message =>
                message.Contains("failed") &&
                message.Contains("Insufficient funds") &&
                message.Contains("Current balance: 50"))),
            Times.Once);
    }

    [Test]
    public void ProcessTransfer_TransactionId_ShouldBeUnique()
    {
        // Arrange
        int fromUserId = 1;
        int toUserId = 2;
        decimal amount = 100;

        var logMessages = new List<string>();

        _ = this._permissionServiceMock
            .Setup(x => x.HasTransferPermission(fromUserId))
            .Returns(true);

        _ = this._accountServiceMock
            .Setup(x => x.GetBalance(fromUserId))
            .Returns(500);

        _ = this._loggerMock
            .Setup(x => x.Log(It.IsAny<string>()))
            .Callback<string>(message => logMessages.Add(message));

        // Act
        _ = this._transactionProcessor.ProcessTransfer(
            fromUserId,
            toUserId,
            amount);

        _ = this._transactionProcessor.ProcessTransfer(
            fromUserId,
            toUserId,
            amount);

        // Assert
        var startedMessages = logMessages
            .Where(message => message.Contains("started"))
            .ToList();

        Assert.That(startedMessages, Has.Count.EqualTo(2));

        string firstTransactionId = startedMessages[0]
            .Split(' ')[1];

        string secondTransactionId = startedMessages[1]
            .Split(' ')[1];

        Assert.That(firstTransactionId, Is.Not.EqualTo(secondTransactionId));
    }

    [Test]
    public void ProcessTransfer_ErrorMessages_ShouldBeProperlyFormatted()
    {
        // Arrange
        int fromUserId = 1;
        int toUserId = 2;
        decimal amount = 100;

        _ = this._permissionServiceMock
            .Setup(x => x.HasTransferPermission(fromUserId))
            .Returns(true);

        _ = this._accountServiceMock
            .Setup(x => x.GetBalance(fromUserId))
            .Returns(500);

        _ = this._transactionServiceMock
            .Setup(x => x.Transfer(fromUserId, toUserId, amount))
            .Throws(new Exception("Transfer failed"));

        // Act
        bool result = this._transactionProcessor.ProcessTransfer(
            fromUserId,
            toUserId,
            amount);

        // Assert
        Assert.That(result, Is.False);

        this._loggerMock.Verify(
            x => x.Log(It.Is<string>(message =>
                message.Contains("failed with error: Transfer failed"))),
            Times.Once);
    }

    [Test]
    public void ProcessTransfer_ExceptionMessages_ShouldBeProperlyFormatted()
    {
        // Arrange
        int fromUserId = 1;
        int toUserId = 2;
        decimal amount = 100;

        string exceptionMessage = "Database connection failed";

        _ = this._permissionServiceMock
            .Setup(x => x.HasTransferPermission(fromUserId))
            .Returns(true);

        _ = this._accountServiceMock
            .Setup(x => x.GetBalance(fromUserId))
            .Returns(500);

        _ = this._transactionServiceMock
            .Setup(x => x.Transfer(fromUserId, toUserId, amount))
            .Throws(new Exception(exceptionMessage));

        // Act
        bool result = this._transactionProcessor.ProcessTransfer(
            fromUserId,
            toUserId,
            amount);

        // Assert
        Assert.That(result, Is.False);

        this._loggerMock.Verify(
            x => x.Log(It.Is<string>(message =>
                message.Contains($"failed with error: {exceptionMessage}"))),
            Times.Once);
    }
}
