using AutoMapper;

using Moq;

using ReUse.Application.Exceptions;
using ReUse.Application.Interfaces;
using ReUse.Application.Interfaces.Repository;
using ReUse.Application.Mappers;
using ReUse.Application.Services;
using ReUse.Domain.Entities;

namespace ReUse.Tests.Services;

public class ActivityServiceTests
{
    // ── Mocks ────────────────────────────────────────────────────────────────
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IActivityRepository> _activityRepo;
    private readonly IMapper _mapper;
    private readonly ActivityService _sut;

    public ActivityServiceTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _activityRepo = new Mock<IActivityRepository>();

        _unitOfWork.Setup(u => u.activities).Returns(_activityRepo.Object);

        // Real AutoMapper with real production profile — mapping bugs are caught,
        // not hidden by a mock.
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ActivityProfile());
        });
        _mapper = config.CreateMapper();

        _sut = new ActivityService(_unitOfWork.Object, _mapper);
    }

    // ── Shared factory ───────────────────────────────────────────────────────
    private static ActivityEvent CreateActivityEvent(
        Guid? id = null,
        Guid? userId = null,
        Guid? productId = null,
        string type = "VIEW",
        string? description = null,
        string? metadata = null)
    {
        return new ActivityEvent
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            ProductId = productId,
            Type = type,
            Description = description,
            Metadata = metadata,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
    }

    // ════════════════════════════════════════════════════════════════════════
    //  GetActivityByIdAsync
    //
    //  What the service actually does:
    //    1. Calls activities.GetByIdAsync(activityId)
    //    2. Returns null when entity is null
    //    3. Otherwise maps ActivityEvent → ActivityEventDto via AutoMapper
    // ════════════════════════════════════════════════════════════════════════

    // HAPPY PATH — all fields mapped correctly
    [Fact]
    public async Task GetActivityByIdAsync_ReturnsMappedDto_WhenActivityExists()
    {
        var activityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var entity = CreateActivityEvent(
            id: activityId,
            userId: userId,
            productId: productId,
            type: "PURCHASE",
            description: "Bought item");

        _activityRepo
            .Setup(r => r.GetByIdAsync(activityId))
            .ReturnsAsync(entity);

        var result = await _sut.GetActivityByIdAsync(activityId);

        Assert.NotNull(result);
        Assert.Equal(activityId, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(productId, result.ProductId);
        Assert.Equal("PURCHASE", result.Type);
        Assert.Equal("Bought item", result.Description);

        _activityRepo.Verify(r => r.GetByIdAsync(activityId), Times.Once);
    }

    // EDGE CASE — repository returns null → service returns null (not an exception)
    [Fact]
    public async Task GetActivityByIdAsync_ReturnsNull_WhenActivityDoesNotExist()
    {
        var activityId = Guid.NewGuid();

        _activityRepo
            .Setup(r => r.GetByIdAsync(activityId))
            .ReturnsAsync((ActivityEvent?)null);

        var result = await _sut.GetActivityByIdAsync(activityId);

        Assert.Null(result);
    }

    // EDGE CASE — nullable fields (ProductId, Description) pass through as null
    [Fact]
    public async Task GetActivityByIdAsync_ReturnsNullForOptionalFields_WhenNotSet()
    {
        var activityId = Guid.NewGuid();
        var entity = CreateActivityEvent(id: activityId, productId: null, description: null);

        _activityRepo.Setup(r => r.GetByIdAsync(activityId)).ReturnsAsync(entity);

        var result = await _sut.GetActivityByIdAsync(activityId);

        Assert.NotNull(result);
        Assert.Null(result.ProductId);
        Assert.Null(result.Description);
    }

    // EDGE CASE — the exact activityId is forwarded, not a default or wrong variable
    [Fact]
    public async Task GetActivityByIdAsync_ForwardsExactActivityId_ToRepository()
    {
        var activityId = Guid.NewGuid();
        var entity = CreateActivityEvent(id: activityId);

        _activityRepo.Setup(r => r.GetByIdAsync(activityId)).ReturnsAsync(entity);

        await _sut.GetActivityByIdAsync(activityId);

        _activityRepo.Verify(r => r.GetByIdAsync(activityId), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  GetUserActivitiesAsync
    //
    //  What the service actually does:
    //    1. Throws BadRequestException when userId == Guid.Empty
    //    2. Throws BadRequestException when limit <= 0 or limit > 1000
    //    3. Calls activities.GetByUserIdAsync(userId, limit)
    //    4. Maps List<ActivityEvent> → List<ActivityEventDto>
    //
    //  GUARD NOTE:
    //  The two guard clauses are independently testable and represent real
    //  API contract invariants worth pinning.
    // ════════════════════════════════════════════════════════════════════════

    // HAPPY PATH — returns mapped list for a valid user
    [Fact]
    public async Task GetUserActivitiesAsync_ReturnsMappedList_WhenUserHasActivities()
    {
        var userId = Guid.NewGuid();
        var activities = new List<ActivityEvent>
        {
            CreateActivityEvent(userId: userId, type: "VIEW"),
            CreateActivityEvent(userId: userId, type: "FAVORITE"),
            CreateActivityEvent(userId: userId, type: "PURCHASE"),
        };

        _activityRepo
            .Setup(r => r.GetByUserIdAsync(userId, 50))
            .ReturnsAsync(activities);

        var result = await _sut.GetUserActivitiesAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.All(result, dto => Assert.Equal(userId, dto.UserId));
        Assert.Equal("VIEW", result[0].Type);
        Assert.Equal("FAVORITE", result[1].Type);
        Assert.Equal("PURCHASE", result[2].Type);

        _activityRepo.Verify(r => r.GetByUserIdAsync(userId, 50), Times.Once);
    }

    // HAPPY PATH — empty list is a valid result, not an error
    [Fact]
    public async Task GetUserActivitiesAsync_ReturnsEmptyList_WhenUserHasNoActivities()
    {
        var userId = Guid.NewGuid();

        _activityRepo
            .Setup(r => r.GetByUserIdAsync(userId, 50))
            .ReturnsAsync(new List<ActivityEvent>());

        var result = await _sut.GetUserActivitiesAsync(userId);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // HAPPY PATH — custom limit is forwarded to the repository
    [Fact]
    public async Task GetUserActivitiesAsync_ForwardsCustomLimit_ToRepository()
    {
        var userId = Guid.NewGuid();
        const int limit = 25;

        _activityRepo
            .Setup(r => r.GetByUserIdAsync(userId, limit))
            .ReturnsAsync(new List<ActivityEvent>());

        await _sut.GetUserActivitiesAsync(userId, limit);

        _activityRepo.Verify(r => r.GetByUserIdAsync(userId, limit), Times.Once);
    }

    // GUARD — Guid.Empty userId throws BadRequestException
    [Fact]
    public async Task GetUserActivitiesAsync_ThrowsBadRequest_WhenUserIdIsEmpty()
    {
        await Assert.ThrowsAsync<BadRequestException>(
            () => _sut.GetUserActivitiesAsync(Guid.Empty));

        _activityRepo.Verify(r => r.GetByUserIdAsync(
            It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    // GUARD — limit of zero throws BadRequestException
    [Fact]
    public async Task GetUserActivitiesAsync_ThrowsBadRequest_WhenLimitIsZero()
    {
        var userId = Guid.NewGuid();

        await Assert.ThrowsAsync<BadRequestException>(
            () => _sut.GetUserActivitiesAsync(userId, 0));
        _activityRepo.Verify(
            r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<int>()),
            Times.Never);
    }

    // GUARD — negative limit throws BadRequestException
    [Fact]
    public async Task GetUserActivitiesAsync_ThrowsBadRequest_WhenLimitIsNegative()
    {
        var userId = Guid.NewGuid();

        await Assert.ThrowsAsync<BadRequestException>(
            () => _sut.GetUserActivitiesAsync(userId, -1));
        _activityRepo.Verify(
            r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<int>()),
            Times.Never);
    }

    // GUARD — limit over 1000 throws BadRequestException
    [Fact]
    public async Task GetUserActivitiesAsync_ThrowsBadRequest_WhenLimitExceeds1000()
    {
        var userId = Guid.NewGuid();

        await Assert.ThrowsAsync<BadRequestException>(
            () => _sut.GetUserActivitiesAsync(userId, 1001));
        _activityRepo.Verify(
            r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<int>()),
            Times.Never);
    }

    // BOUNDARY — limit of exactly 1 is valid (lower boundary)
    [Fact]
    public async Task GetUserActivitiesAsync_Succeeds_WhenLimitIsOne()
    {
        var userId = Guid.NewGuid();

        _activityRepo
            .Setup(r => r.GetByUserIdAsync(userId, 1))
            .ReturnsAsync(new List<ActivityEvent>());

        var result = await _sut.GetUserActivitiesAsync(userId, 1);

        Assert.NotNull(result);
        _activityRepo.Verify(r => r.GetByUserIdAsync(userId, 1), Times.Once);
    }

    // BOUNDARY — limit of exactly 1000 is valid (upper boundary)
    [Fact]
    public async Task GetUserActivitiesAsync_Succeeds_WhenLimitIs1000()
    {
        var userId = Guid.NewGuid();

        _activityRepo
            .Setup(r => r.GetByUserIdAsync(userId, 1000))
            .ReturnsAsync(new List<ActivityEvent>());

        var result = await _sut.GetUserActivitiesAsync(userId, 1000);

        Assert.NotNull(result);
        _activityRepo.Verify(r => r.GetByUserIdAsync(userId, 1000), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CreateActivityAsync
    //
    //  What the service actually does:
    //    1. Builds a CreateActivityRequest record from parameters
    //    2. Maps CreateActivityRequest → ActivityEvent via AutoMapper
    //       (Timestamp is set to DateTime.UtcNow by the mapping profile)
    //    3. activities.Add(entity)
    //    4. SaveChangesAsync()
    //
    //  NOTE: The service has no guard clauses — validation is the caller's
    //  responsibility. The only invariant we can pin here is that Add and
    //  SaveChangesAsync are always called, and that the mapped entity carries
    //  the right UserId, ProductId, Type, Description, and Metadata.
    // ════════════════════════════════════════════════════════════════════════

    // HAPPY PATH — entity added and saved with all fields correctly populated
    [Fact]
    public async Task CreateActivityAsync_AddsEntityAndSaves_WithAllFields()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        ActivityEvent? capturedEntity = null;

        _activityRepo
            .Setup(r => r.Add(It.IsAny<ActivityEvent>()))
            .Callback<ActivityEvent>(e => capturedEntity = e);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.CreateActivityAsync(userId, productId, "VIEW", "Viewed item", "{\"source\":\"search\"}");

        Assert.NotNull(capturedEntity);
        Assert.Equal(userId, capturedEntity!.UserId);
        Assert.Equal(productId, capturedEntity.ProductId);
        Assert.Equal("VIEW", capturedEntity.Type);
        Assert.Equal("Viewed item", capturedEntity.Description);
        Assert.Equal("{\"source\":\"search\"}", capturedEntity.Metadata);

        _activityRepo.Verify(r => r.Add(It.IsAny<ActivityEvent>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // HAPPY PATH — optional parameters (productId, description, metadata) can be null
    [Fact]
    public async Task CreateActivityAsync_AddsEntity_WhenOptionalParamsAreNull()
    {
        var userId = Guid.NewGuid();
        ActivityEvent? capturedEntity = null;

        _activityRepo
            .Setup(r => r.Add(It.IsAny<ActivityEvent>()))
            .Callback<ActivityEvent>(e => capturedEntity = e);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.CreateActivityAsync(userId, null, "LOGIN");

        Assert.NotNull(capturedEntity);
        Assert.Equal(userId, capturedEntity!.UserId);
        Assert.Null(capturedEntity.ProductId);
        Assert.Null(capturedEntity.Description);
        Assert.Null(capturedEntity.Metadata);
    }

    // EDGE CASE — Timestamp is set by the mapping profile, not left as default
    [Fact]
    public async Task CreateActivityAsync_SetsTimestampOnEntity_ViaMapper()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        ActivityEvent? capturedEntity = null;

        _activityRepo
            .Setup(r => r.Add(It.IsAny<ActivityEvent>()))
            .Callback<ActivityEvent>(e => capturedEntity = e);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.CreateActivityAsync(Guid.NewGuid(), null, "VIEW");

        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.NotNull(capturedEntity);
        Assert.InRange(capturedEntity!.Timestamp, before, after);
    }

    // EDGE CASE — SaveChangesAsync is called after Add, not before
    // If Add is skipped, the entity is never persisted.
    [Fact]
    public async Task CreateActivityAsync_CallsAddBeforeSave()
    {
        var callOrder = new List<string>();

        _activityRepo
            .Setup(r => r.Add(It.IsAny<ActivityEvent>()))
            .Callback<ActivityEvent>(_ => callOrder.Add("add"));
        _unitOfWork
            .Setup(u => u.SaveChangesAsync())
            .Callback(() => callOrder.Add("save"))
            .ReturnsAsync(1);

        await _sut.CreateActivityAsync(Guid.NewGuid(), null, "VIEW");

        Assert.Equal(new[] { "add", "save" }, callOrder);
    }
}