using Transaction.Domain.Rules;

namespace Transaction.Tests.Unit.UnitTests.Domain.Rules;

public sealed class RuleResultTests
{
    [Test]
    public void Success_ReturnsValidResultWithNoError()
    {
        // Act
        RuleResult result = RuleResult.Success();

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.RequiresReview, Is.False);
        }
    }

    [Test]
    public void Failure_ReturnsInvalidResultWithErrorMessage()
    {
        // Arrange
        const string errorMessage = "Daily limit exceeded";

        // Act
        RuleResult result = RuleResult.Failure(errorMessage);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo(errorMessage));
            Assert.That(result.RequiresReview, Is.False);
        }
    }

    [Test]
    public void NeedsReview_ReturnsValidResultWithReviewFlagAndReason()
    {
        // Arrange
        const string reason = "Transaction amount exceeds high-value threshold and requires review";

        // Act
        RuleResult result = RuleResult.NeedsReview(reason);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ErrorMessage, Is.EqualTo(reason));
            Assert.That(result.RequiresReview, Is.True);
        }
    }

    [Test]
    public void Success_IsDistinctFromFailure()
    {
        // Act
        RuleResult success = RuleResult.Success();
        RuleResult failure = RuleResult.Failure("some error");

        // Assert
        Assert.That(success.IsValid, Is.Not.EqualTo(failure.IsValid));
    }

    [Test]
    public void NeedsReview_IsDistinctFromSuccess_ByRequiresReviewFlag()
    {
        // Act
        RuleResult success = RuleResult.Success();
        RuleResult review = RuleResult.NeedsReview("needs review");

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success.IsValid, Is.True);
            Assert.That(review.IsValid, Is.True);
            Assert.That(success.RequiresReview, Is.False);
            Assert.That(review.RequiresReview, Is.True);
        }
    }

    [Test]
    public void Failure_EmptyErrorMessage_ReturnsInvalidResult()
    {
        // Act
        RuleResult result = RuleResult.Failure(string.Empty);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo(string.Empty));
        }
    }
}
