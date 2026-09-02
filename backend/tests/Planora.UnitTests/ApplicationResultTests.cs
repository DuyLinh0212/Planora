using Planora.Application.Common.Results;

namespace Planora.UnitTests;

public sealed class ApplicationResultTests
{
    [Fact]
    public void ValidationFailurePreservesErrorMetadata()
    {
        var result = ApplicationResult.Failure<string>(ApplicationErrors.Validation("task.title_required", "A title is required.", "title"));
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApplicationErrorType.Validation, error.Type);
        Assert.Equal("title", error.Field);
    }
}
