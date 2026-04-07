using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Warehouse.Common.Models;
using Warehouse.Infrastructure.Controllers;

namespace Warehouse.Infrastructure.Tests.Controllers;

/// <summary>
/// Tests for <see cref="BaseApiController"/> helper methods including ToActionResult
/// and ToCreatedResult for both generic and non-generic Result types.
/// </summary>
[TestFixture]
[Category("SDD-INFRA-001")]
public sealed class BaseApiControllerTests
{
    private TestableApiController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _controller = new TestableApiController();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Test]
    public void ToActionResult_SuccessResultWithValue_ReturnsOkObjectResult()
    {
        // Arrange
        string value = "test-value";
        Result<string> result = Result<string>.Success(value);

        // Act
        IActionResult actionResult = _controller.CallToActionResult(result);

        // Assert
        Assert.That(actionResult, Is.TypeOf<OkObjectResult>());
        OkObjectResult okResult = (OkObjectResult)actionResult;
        Assert.That(okResult.Value, Is.EqualTo(value));
    }

    [Test]
    public void ToActionResult_FailureResult_ReturnsProblemDetailsObjectResult()
    {
        // Arrange
        Result<string> result = Result<string>.Failure("NOT_FOUND", "Item not found", 404);

        // Act
        IActionResult actionResult = _controller.CallToActionResult(result);

        // Assert
        Assert.That(actionResult, Is.TypeOf<ObjectResult>());
        ObjectResult objectResult = (ObjectResult)actionResult;
        Assert.That(objectResult.Value, Is.TypeOf<ProblemDetails>());
    }

    [Test]
    public void ToActionResult_FailureResult_CorrectStatusCode()
    {
        // Arrange
        Result<string> result = Result<string>.Failure("CONFLICT", "Already exists", 409);

        // Act
        IActionResult actionResult = _controller.CallToActionResult(result);

        // Assert
        ObjectResult objectResult = (ObjectResult)actionResult;
        ProblemDetails problemDetails = (ProblemDetails)objectResult.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(objectResult.StatusCode, Is.EqualTo(409));
            Assert.That(problemDetails.Status, Is.EqualTo(409));
            Assert.That(problemDetails.Title, Is.EqualTo("CONFLICT"));
            Assert.That(problemDetails.Detail, Is.EqualTo("Already exists"));
            Assert.That(problemDetails.Type, Is.EqualTo("https://warehouse.local/errors/CONFLICT"));
        });
    }

    [Test]
    public void ToCreatedResult_SuccessResult_ReturnsCreatedAtRouteResult()
    {
        // Arrange
        string value = "created-item";
        Result<string> result = Result<string>.Success(value);

        // Act
        IActionResult actionResult = _controller.CallToCreatedResult(
            result, "GetById", v => new { id = v });

        // Assert
        Assert.That(actionResult, Is.TypeOf<CreatedAtRouteResult>());
        CreatedAtRouteResult createdResult = (CreatedAtRouteResult)actionResult;

        Assert.Multiple(() =>
        {
            Assert.That(createdResult.Value, Is.EqualTo(value));
            Assert.That(createdResult.RouteName, Is.EqualTo("GetById"));
        });
    }

    [Test]
    public void ToCreatedResult_FailureResult_ReturnsProblemDetails()
    {
        // Arrange
        Result<string> result = Result<string>.Failure("VALIDATION_ERROR", "Invalid input", 400);

        // Act
        IActionResult actionResult = _controller.CallToCreatedResult(
            result, "GetById", v => new { id = v });

        // Assert
        Assert.That(actionResult, Is.TypeOf<ObjectResult>());
        ObjectResult objectResult = (ObjectResult)actionResult;
        Assert.That(objectResult.Value, Is.TypeOf<ProblemDetails>());
        Assert.That(objectResult.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public void ToActionResult_NonGeneric_Success_ReturnsNoContent()
    {
        // Arrange
        Result result = Result.Success();

        // Act
        IActionResult actionResult = _controller.CallToActionResultNonGeneric(result);

        // Assert
        Assert.That(actionResult, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public void ToActionResult_NonGeneric_Failure_ReturnsProblemDetails()
    {
        // Arrange
        Result result = Result.Failure("FORBIDDEN", "Access denied", 403);

        // Act
        IActionResult actionResult = _controller.CallToActionResultNonGeneric(result);

        // Assert
        Assert.That(actionResult, Is.TypeOf<ObjectResult>());
        ObjectResult objectResult = (ObjectResult)actionResult;
        ProblemDetails problemDetails = (ProblemDetails)objectResult.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(objectResult.StatusCode, Is.EqualTo(403));
            Assert.That(problemDetails.Status, Is.EqualTo(403));
            Assert.That(problemDetails.Title, Is.EqualTo("FORBIDDEN"));
            Assert.That(problemDetails.Detail, Is.EqualTo("Access denied"));
        });
    }

    [Test]
    public void ToActionResult_SuccessResultWithComplexValue_ReturnsOkWithValue()
    {
        // Arrange
        TestDto dto = new() { Id = 42, Name = "Test Item" };
        Result<TestDto> result = Result<TestDto>.Success(dto);

        // Act
        IActionResult actionResult = _controller.CallToActionResult(result);

        // Assert
        Assert.That(actionResult, Is.TypeOf<OkObjectResult>());
        OkObjectResult okResult = (OkObjectResult)actionResult;
        TestDto returnedDto = (TestDto)okResult.Value!;

        Assert.Multiple(() =>
        {
            Assert.That(returnedDto.Id, Is.EqualTo(42));
            Assert.That(returnedDto.Name, Is.EqualTo("Test Item"));
        });
    }

    /// <summary>
    /// Concrete test controller that exposes the protected BaseApiController methods for testing.
    /// </summary>
    private sealed class TestableApiController : BaseApiController
    {
        /// <summary>
        /// Exposes the protected ToActionResult for generic Result.
        /// </summary>
        public IActionResult CallToActionResult<T>(Result<T> result)
        {
            return ToActionResult(result);
        }

        /// <summary>
        /// Exposes the protected ToActionResult for non-generic Result.
        /// </summary>
        public IActionResult CallToActionResultNonGeneric(Result result)
        {
            return ToActionResult(result);
        }

        /// <summary>
        /// Exposes the protected ToCreatedResult.
        /// </summary>
        public IActionResult CallToCreatedResult<T>(Result<T> result, string routeName, Func<T, object> routeValues)
        {
            return ToCreatedResult(result, routeName, routeValues);
        }
    }

    /// <summary>
    /// Simple DTO used for testing complex value serialization.
    /// </summary>
    private sealed class TestDto
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
