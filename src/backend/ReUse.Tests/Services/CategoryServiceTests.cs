using AutoMapper;

using Microsoft.AspNetCore.Http;

using Moq;

using ReUse.Application.DTOs;
using ReUse.Application.DTOs.Categories;
using ReUse.Application.DTOs.Users.UserProfile;
using ReUse.Application.Exceptions;
using ReUse.Application.Interfaces;
using ReUse.Application.Interfaces.Repository;
using ReUse.Application.Interfaces.Services;
using ReUse.Application.Interfaces.Services.External;
using ReUse.Application.Mappers;
using ReUse.Application.Services;
using ReUse.Domain.Entities;

namespace ReUse.Tests.Services;

public class CategoryServiceTests
{
    // ── Mocks ────────────────────────────────────────────────────────────────
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ICategoryRepository> _categoryRepo;
    private readonly Mock<IProductRepository> _productRepo;
    private readonly Mock<ICloudinaryService> _cloudinary;
    private readonly Mock<IImageValidator> _imageValidator;
    private readonly Mock<ISystemActivityLogService> _activityLog;
    private readonly IMapper _mapper;
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _categoryRepo = new Mock<ICategoryRepository>();
        _productRepo = new Mock<IProductRepository>();
        _cloudinary = new Mock<ICloudinaryService>();
        _imageValidator = new Mock<IImageValidator>();
        _activityLog = new Mock<ISystemActivityLogService>();

        _unitOfWork.Setup(u => u.Category).Returns(_categoryRepo.Object);
        _unitOfWork.Setup(u => u.Product).Returns(_productRepo.Object);

        // Real AutoMapper with the real production profile
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new CategoryProfile());
        });
        _mapper = config.CreateMapper();

        _sut = new CategoryService(
            _unitOfWork.Object,
            _mapper,
            _cloudinary.Object,
            _imageValidator.Object,
            _activityLog.Object);
    }

    // ── Shared factory ───────────────────────────────────────────────────────
    private static Category CreateCategory(
        Guid? id = null,
        string name = "Electronics",
        string slug = "electronics",
        string? description = null,
        string? iconUrl = null,
        string? iconPublicId = null,
        bool isActive = true,
        Guid? parentId = null)
    {
        return new Category
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Description = description,
            IconUrl = iconUrl,
            IconPublicId = iconPublicId,
            IsActive = isActive,
            ParentId = parentId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static IFormFile CreateFormFile(string name = "icon.png")
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(name);
        mock.Setup(f => f.Length).Returns(512);
        return mock.Object;
    }

    private static PagedResult<Category> EmptyPagedResult(int page = 1, int size = 10) =>
        new PagedResult<Category>
        {
            Data = new List<Category>(),
            PageNumber = page,
            PageSize = size,
            TotalRecords = 0
        };

    [Fact]
    public async Task CreateAsync_LogsActivity_WhenActorAdminIdProvided()
    {
        var adminId = Guid.NewGuid();

        var request = new CreateCategoryRequest(
            "Electronics",
            "electronics",
            null,
            null,
            null);

        _categoryRepo.Setup(r => r.NameExistsAsync(request.Name, null))
            .ReturnsAsync(false);

        _categoryRepo.Setup(r => r.SlugExistsAsync(request.Slug, null))
            .ReturnsAsync(false);

        await _sut.CreateAsync(request, adminId);

        _activityLog.Verify(
            x => x.LogCategoryCreatedAsync(
                adminId,
                It.IsAny<Guid>(),
                "Electronics"),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DoesNotLogActivity_WhenActorAdminIdIsNull()
    {
        var request = new CreateCategoryRequest(
            "Electronics",
            "electronics",
            null,
            null,
            null);

        _categoryRepo.Setup(r => r.NameExistsAsync(request.Name, null))
            .ReturnsAsync(false);

        _categoryRepo.Setup(r => r.SlugExistsAsync(request.Slug, null))
            .ReturnsAsync(false);

        await _sut.CreateAsync(request);

        _activityLog.Verify(
            x => x.LogCategoryCreatedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>()),
            Times.Never);
    }
    // ════════════════════════════════════════════════════════════════════════
    //  GetCategoriesAsync
    //
    //  What the service actually does:
    //    1. Calls Category.GetAllAsync(filterParams) → PagedResult<Category>
    //    2. Maps Data list → List<CategoryResponse>
    //    3. Fetches active product counts and merges them into each DTO
    //    4. Returns a new PagedResult<CategoryResponse> preserving pagination meta
    // ════════════════════════════════════════════════════════════════════════

    // HAPPY PATH — returns mapped DTOs with pagination meta preserved
    [Fact]
    public async Task GetCategoriesAsync_ReturnsMappedDtos_WithCorrectPaginationMeta()
    {
        var filterParams = new CategoriesFilterParams();
        var categories = new List<Category>
        {
            CreateCategory(name: "Electronics", slug: "electronics"),
            CreateCategory(name: "Books", slug: "books"),
        };
        var pagedResult = new PagedResult<Category>
        {
            Data = categories,
            PageNumber = 2,
            PageSize = 10,
            TotalRecords = 25,
        };

        _categoryRepo
            .Setup(r => r.GetAllAsync(filterParams, default))
            .ReturnsAsync(pagedResult);
        _productRepo
            .Setup(r => r.GetActiveCountsByCategoryAsync())
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetCategoriesAsync(filterParams);

        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalRecords);
        Assert.Equal("Electronics", result.Data[0].Name);
        Assert.Equal("Books", result.Data[1].Name);
    }

    // EDGE CASE — ProductCount is populated from the counts dictionary
    [Fact]
    public async Task GetCategoriesAsync_PopulatesProductCount_FromCountsDictionary()
    {
        var catId = Guid.NewGuid();
        var category = CreateCategory(id: catId);
        var pagedResult = new PagedResult<Category>
        {
            Data = new List<Category> { category },
            PageNumber = 1,
            PageSize = 10,
            TotalRecords = 1,
        };

        _categoryRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CategoriesFilterParams>(), default))
            .ReturnsAsync(pagedResult);
        _productRepo
            .Setup(r => r.GetActiveCountsByCategoryAsync())
            .ReturnsAsync(new Dictionary<Guid, int> { { catId, 42 } });

        var result = await _sut.GetCategoriesAsync(new CategoriesFilterParams());

        Assert.Equal(42, result.Data[0].ProductCount);
    }

    // EDGE CASE — category not in counts dictionary gets ProductCount of 0
    [Fact]
    public async Task GetCategoriesAsync_SetsProductCountToZero_WhenCategoryNotInDictionary()
    {
        var category = CreateCategory();
        var pagedResult = new PagedResult<Category>
        {
            Data = new List<Category> { category },
            PageNumber = 1,
            PageSize = 10,
            TotalRecords = 1,
        };

        _categoryRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CategoriesFilterParams>(), default))
            .ReturnsAsync(pagedResult);
        _productRepo
            .Setup(r => r.GetActiveCountsByCategoryAsync())
            .ReturnsAsync(new Dictionary<Guid, int>()); // category absent

        var result = await _sut.GetCategoriesAsync(new CategoriesFilterParams());

        Assert.Equal(0, result.Data[0].ProductCount);
    }

    // EDGE CASE — empty page returns empty list, meta still preserved
    [Fact]
    public async Task GetCategoriesAsync_ReturnsEmptyList_WhenNoCategories()
    {
        _categoryRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CategoriesFilterParams>(), default))
            .ReturnsAsync(EmptyPagedResult());
        _productRepo
            .Setup(r => r.GetActiveCountsByCategoryAsync())
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetCategoriesAsync(new CategoriesFilterParams());

        Assert.Empty(result.Data);
        Assert.Equal(0, result.TotalRecords);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  GetCategoryTreeAsync
    //
    //  What the service actually does:
    //    1. Calls Category.GetAllAsync() (no filter)
    //    2. When includeInactive=false, filters out IsActive=false entries
    //    3. Maps to DTOs, merges product counts
    //    4. Builds a parent-child tree: roots have ParentId=null,
    //       children are added to their parent's Subcategories list
    // ════════════════════════════════════════════════════════════════════════

    // HAPPY PATH — builds tree with roots and children
    [Fact]
    public async Task GetCategoryTreeAsync_BuildsTree_WithCorrectHierarchy()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var root = CreateCategory(id: rootId, name: "Electronics", slug: "electronics");
        var child = CreateCategory(id: childId, name: "Phones", slug: "phones", parentId: rootId);

        _categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { root, child });
        _productRepo.Setup(r => r.GetActiveCountsByCategoryAsync())
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetCategoryTreeAsync(includeInactive: true);

        Assert.Single(result); // only root at top level
        Assert.Equal("Electronics", result[0].Name);
        Assert.Single(result[0].Subcategories);
        Assert.Equal("Phones", result[0].Subcategories[0].Name);
    }

    // HAPPY PATH — multiple root categories, each with independent children
    [Fact]
    public async Task GetCategoryTreeAsync_ReturnsMultipleRoots_WhenNoCategoriesShareParent()
    {
        var cat1 = CreateCategory(name: "Electronics", slug: "electronics");
        var cat2 = CreateCategory(name: "Books", slug: "books");

        _categoryRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Category> { cat1, cat2 });
        _productRepo.Setup(r => r.GetActiveCountsByCategoryAsync())
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetCategoryTreeAsync(includeInactive: true);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Empty(r.Subcategories));
    }

    // EDGE CASE — inactive categories excluded when includeInactive=false (default)
    [Fact]
    public async Task GetCategoryTreeAsync_ExcludesInactiveCategories_ByDefault()
    {
        var active = CreateCategory(name: "Active", slug: "active", isActive: true);
        var inactive = CreateCategory(name: "Inactive", slug: "inactive", isActive: false);

        _categoryRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Category> { active, inactive });
        _productRepo.Setup(r => r.GetActiveCountsByCategoryAsync())
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetCategoryTreeAsync(); // includeInactive defaults to false

        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    // EDGE CASE — includeInactive=true returns all categories
    [Fact]
    public async Task GetCategoryTreeAsync_IncludesInactiveCategories_WhenRequested()
    {
        var active = CreateCategory(name: "Active", slug: "active", isActive: true);
        var inactive = CreateCategory(name: "Inactive", slug: "inactive", isActive: false);

        _categoryRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Category> { active, inactive });
        _productRepo.Setup(r => r.GetActiveCountsByCategoryAsync())
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetCategoryTreeAsync(includeInactive: true);

        Assert.Equal(2, result.Count);
    }

    // EDGE CASE — child whose parent is inactive is orphaned (parent filtered out)
    // The service doesn't add orphaned children to roots; they simply disappear.
    [Fact]
    public async Task GetCategoryTreeAsync_DropsOrphanedChildren_WhenParentIsFiltered()
    {
        var inactiveParentId = Guid.NewGuid();
        var inactiveParent = CreateCategory(id: inactiveParentId, isActive: false, slug: "p");
        var child = CreateCategory(parentId: inactiveParentId, name: "Child", slug: "child");

        _categoryRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Category> { inactiveParent, child });
        _productRepo.Setup(r => r.GetActiveCountsByCategoryAsync())
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetCategoryTreeAsync(); // includeInactive=false

        // Neither the inactive parent nor the child should appear
        Assert.Empty(result);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  GetByIdAsync
    //
    //  What the service actually does:
    //    1. Category.GetByIdAsync(id)
    //    2. Throws NotFoundException when null
    //    3. Maps to DTO
    //    4. Fetches active product count and merges it
    // ════════════════════════════════════════════════════════════════════════

    // HAPPY PATH — returns mapped DTO with product count
    [Fact]
    public async Task GetByIdAsync_ReturnsMappedDto_WithProductCount()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id, name: "Electronics", slug: "electronics");

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _productRepo.Setup(r => r.GetActiveCountForCategoryAsync(id)).ReturnsAsync(7);

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("Electronics", result.Name);
        Assert.Equal(7, result.ProductCount);

        _categoryRepo.Verify(r => r.GetByIdAsync(id), Times.Once);
        _productRepo.Verify(r => r.GetActiveCountForCategoryAsync(id), Times.Once);
    }

    // GUARD — throws NotFoundException when category not found
    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenCategoryDoesNotExist()
    {
        var id = Guid.NewGuid();
        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.GetByIdAsync(id));

        // Product count should never be fetched if category doesn't exist
        _productRepo.Verify(r => r.GetActiveCountForCategoryAsync(It.IsAny<Guid>()), Times.Never);
    }

    // EDGE CASE — exact id is forwarded to both repositories
    [Fact]
    public async Task GetByIdAsync_ForwardsExactId_ToBothRepositories()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id);

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _productRepo.Setup(r => r.GetActiveCountForCategoryAsync(id)).ReturnsAsync(0);

        await _sut.GetByIdAsync(id);

        _categoryRepo.Verify(r => r.GetByIdAsync(id), Times.Once);
        _productRepo.Verify(r => r.GetActiveCountForCategoryAsync(id), Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CreateAsync
    //
    //  What the service actually does:
    //    1. If ParentId set → checks parent exists, throws NotFoundException if not
    //    2. Checks name uniqueness → throws ConflictException if duplicate
    //    3. Checks slug uniqueness → throws ConflictException if duplicate
    //    4. Maps CreateCategoryRequest → Category
    //    5. Category.Add(entity) + SaveChangesAsync()
    //    6. Returns mapped CategoryResponse
    //
    //  GUARD ORDER NOTE:
    //  Parent check → name check → slug check. Each is independently testable.
    // ════════════════════════════════════════════════════════════════════════

    // HAPPY PATH — creates category without a parent
    [Fact]
    public async Task CreateAsync_AddsAndSaves_WhenRequestIsValid()
    {
        var request = new CreateCategoryRequest("Electronics", "electronics", null, null, null);

        _categoryRepo.Setup(r => r.NameExistsAsync(request.Name, null)).ReturnsAsync(false);
        _categoryRepo.Setup(r => r.SlugExistsAsync(request.Slug, null)).ReturnsAsync(false);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Electronics", result.Name);
        Assert.Equal("electronics", result.Slug);

        _categoryRepo.Verify(r => r.Add(It.IsAny<Category>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // HAPPY PATH — creates category with a valid parent
    [Fact]
    public async Task CreateAsync_AddsAndSaves_WhenValidParentIdProvided()
    {
        var parentId = Guid.NewGuid();
        var request = new CreateCategoryRequest("Phones", "phones", null, null, parentId);

        _categoryRepo.Setup(r => r.ExistsAsync(parentId)).ReturnsAsync(true);
        _categoryRepo.Setup(r => r.NameExistsAsync(request.Name, null)).ReturnsAsync(false);
        _categoryRepo.Setup(r => r.SlugExistsAsync(request.Slug, null)).ReturnsAsync(false);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(result);
        _categoryRepo.Verify(r => r.ExistsAsync(parentId), Times.Once);
    }

    // GUARD — parent not found throws NotFoundException
    [Fact]
    public async Task CreateAsync_ThrowsNotFound_WhenParentDoesNotExist()
    {
        var parentId = Guid.NewGuid();
        var request = new CreateCategoryRequest("Phones", "phones", null, null, parentId);

        _categoryRepo.Setup(r => r.ExistsAsync(parentId)).ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.CreateAsync(request));

        _categoryRepo.Verify(r => r.Add(It.IsAny<Category>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // GUARD — duplicate name throws ConflictException
    [Fact]
    public async Task CreateAsync_ThrowsConflict_WhenNameAlreadyExists()
    {
        var request = new CreateCategoryRequest("Electronics", "electronics-2", null, null, null);

        _categoryRepo.Setup(r => r.NameExistsAsync(request.Name, null)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.CreateAsync(request));

        _categoryRepo.Verify(r => r.Add(It.IsAny<Category>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // GUARD — duplicate slug throws ConflictException
    [Fact]
    public async Task CreateAsync_ThrowsConflict_WhenSlugAlreadyExists()
    {
        var request = new CreateCategoryRequest("New Category", "electronics", null, null, null);

        _categoryRepo.Setup(r => r.NameExistsAsync(request.Name, null)).ReturnsAsync(false);
        _categoryRepo.Setup(r => r.SlugExistsAsync(request.Slug, null)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.CreateAsync(request));

        _categoryRepo.Verify(r => r.Add(It.IsAny<Category>()), Times.Never);
    }

    // EDGE CASE — parent check skipped when no ParentId given
    [Fact]
    public async Task CreateAsync_DoesNotCheckParentExistence_WhenParentIdIsNull()
    {
        var request = new CreateCategoryRequest("Root", "root", null, null, null);

        _categoryRepo.Setup(r => r.NameExistsAsync(request.Name, null)).ReturnsAsync(false);
        _categoryRepo.Setup(r => r.SlugExistsAsync(request.Slug, null)).ReturnsAsync(false);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.CreateAsync(request);

        _categoryRepo.Verify(r => r.ExistsAsync(It.IsAny<Guid>()), Times.Never);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  UpdateAsync
    //
    //  What the service actually does:
    //    1. GetByIdAsync(id) → NotFoundException if null
    //    2. If ParentId set AND equals id → ConflictException (self-parent)
    //    3. If ParentId set → ExistsAsync → NotFoundException if missing
    //    4. Name uniqueness check (only if name not null/whitespace), excludes self
    //    5. Slug uniqueness check (only if slug not null/whitespace), excludes self
    //    6. Maps UpdateCategoryRequest → entity (null-conditional per CategoryProfile)
    //    7. Sets UpdatedAt = UtcNow
    //    8. Update + SaveChangesAsync → returns mapped CategoryResponse
    // ════════════════════════════════════════════════════════════════════════

    // HAPPY PATH — updates name and slug, returns updated DTO
    [Fact]
    public async Task UpdateAsync_UpdatesEntity_WhenRequestIsValid()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id);

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _categoryRepo.Setup(r => r.NameExistsAsync("NewName", id)).ReturnsAsync(false);
        _categoryRepo.Setup(r => r.SlugExistsAsync("new-name", id)).ReturnsAsync(false);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var request = new UpdateCategoryRequest("NewName", "new-name", null, null, null, null);

        var result = await _sut.UpdateAsync(id, request);

        Assert.NotNull(result);
        Assert.Equal("NewName", result.Name);
        Assert.Equal("new-name", result.Slug);

        _categoryRepo.Verify(r => r.Update(category), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // GUARD — throws NotFoundException when category not found
    [Fact]
    public async Task UpdateAsync_ThrowsNotFound_WhenCategoryDoesNotExist()
    {
        var id = Guid.NewGuid();
        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(id, new UpdateCategoryRequest(null, null, null, null, null, null)));

        _categoryRepo.Verify(r => r.Update(It.IsAny<Category>()), Times.Never);
    }

    // GUARD — category cannot be its own parent
    [Fact]
    public async Task UpdateAsync_ThrowsConflict_WhenParentIdEqualsCategoryId()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id);

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);

        var request = new UpdateCategoryRequest(null, null, null, null, null, id); // ParentId == id

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.UpdateAsync(id, request));
    }

    // GUARD — throws NotFoundException when parent category not found
    [Fact]
    public async Task UpdateAsync_ThrowsNotFound_WhenParentDoesNotExist()
    {
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var category = CreateCategory(id: id);

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _categoryRepo.Setup(r => r.ExistsAsync(parentId)).ReturnsAsync(false);

        var request = new UpdateCategoryRequest(null, null, null, null, null, parentId);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(id, request));
    }

    // GUARD — duplicate name (excluding self) throws ConflictException
    [Fact]
    public async Task UpdateAsync_ThrowsConflict_WhenNameAlreadyTakenByAnother()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id);

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _categoryRepo.Setup(r => r.NameExistsAsync("TakenName", id)).ReturnsAsync(true);

        var request = new UpdateCategoryRequest("TakenName", null, null, null, null, null);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.UpdateAsync(id, request));
    }

    // GUARD — duplicate slug (excluding self) throws ConflictException
    [Fact]
    public async Task UpdateAsync_ThrowsConflict_WhenSlugAlreadyTakenByAnother()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id);

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _categoryRepo.Setup(r => r.NameExistsAsync(It.IsAny<string>(), id)).ReturnsAsync(false);
        _categoryRepo.Setup(r => r.SlugExistsAsync("taken-slug", id)).ReturnsAsync(true);

        var request = new UpdateCategoryRequest(null, "taken-slug", null, null, null, null);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.UpdateAsync(id, request));
    }

    // EDGE CASE — null name and slug skip their uniqueness checks entirely
    [Fact]
    public async Task UpdateAsync_SkipsUniquenessChecks_WhenNameAndSlugAreNull()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id);

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var request = new UpdateCategoryRequest(null, null, "Updated desc", null, null, null);

        await _sut.UpdateAsync(id, request);

        _categoryRepo.Verify(r => r.NameExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
        _categoryRepo.Verify(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
    }

    // EDGE CASE — UpdatedAt is set to a recent UtcNow value
    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt_ToUtcNow()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id);
        var before = DateTime.UtcNow.AddSeconds(-1);

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.UpdateAsync(id, new UpdateCategoryRequest(null, null, null, null, null, null));

        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.NotNull(category.UpdatedAt);
        Assert.InRange(category.UpdatedAt!.Value, before, after);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DeleteAsync
    //
    //  What the service actually does:
    //    1. GetByIdAsync(id) → NotFoundException if null
    //    2. Category.Remove(entity)
    //    3. SaveChangesAsync()
    //
    //  NOTE: There is no Cloudinary cleanup here — the icon is left in storage.
    //  This documents the gap so it's explicit, not hidden.
    // ════════════════════════════════════════════════════════════════════════

    // HAPPY PATH — removes and saves
    [Fact]
    public async Task DeleteAsync_RemovesEntityAndSaves_WhenCategoryExists()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id);

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.DeleteAsync(id);

        _categoryRepo.Verify(r => r.Remove(category), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // GUARD — throws NotFoundException when category not found
    [Fact]
    public async Task DeleteAsync_ThrowsNotFound_WhenCategoryDoesNotExist()
    {
        var id = Guid.NewGuid();
        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.DeleteAsync(id));

        _categoryRepo.Verify(r => r.Remove(It.IsAny<Category>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  UploadIconAsync
    //
    //  What the service actually does:
    //    1. GetByIdAsync(id) → NotFoundException if null
    //    2. _imageValidator.Validate(file) — throws on invalid file
    //    3. If old IconPublicId not null/whitespace → DeleteAsync(oldPublicId)
    //    4. Cloudinary.UpdateAsync(file, "categories/{id}") → {Url, PublicId}
    //    5. Writes new url+publicId to entity fields
    //    6. Sets UpdatedAt = UtcNow
    //    7. Category.Update + SaveChangesAsync
    //    8. Returns mapped CategoryResponse
    //
    //  ORDER NOTE: The old icon is deleted BEFORE the new one is uploaded.
    //  This differs from UserService which saves first then deletes.
    // ════════════════════════════════════════════════════════════════════════

    // HAPPY PATH — uploads icon, old one deleted first
    [Fact]
    public async Task UploadIconAsync_UploadsNewIcon_AndDeletesOldOne()
    {
        var id = Guid.NewGuid();
        var oldPublicId = "old_icon_abc";
        var category = CreateCategory(id: id, iconUrl: "https://old.com/icon.png", iconPublicId: oldPublicId);

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _cloudinary
            .Setup(c => c.UpdateAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(new ImageUpdatedResponse("https://new.com/icon.png", "new_icon_xyz"));
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UploadIconAsync(id, CreateFormFile());

        Assert.NotNull(result);
        Assert.Equal("https://new.com/icon.png", category.IconUrl);
        Assert.Equal("new_icon_xyz", category.IconPublicId);

        _cloudinary.Verify(c => c.DeleteAsync(oldPublicId), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _categoryRepo.Verify(
            r => r.Update(category),
            Times.Once);
    }

    // HAPPY PATH — first-time upload (no old icon) → DeleteAsync not called
    [Fact]
    public async Task UploadIconAsync_DoesNotCallDelete_WhenNoOldIconExists()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id); // iconPublicId is null

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _cloudinary
            .Setup(c => c.UpdateAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(new ImageUpdatedResponse("https://new.com/icon.png", "new_id"));
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.UploadIconAsync(id, CreateFormFile());

        _cloudinary.Verify(c => c.DeleteAsync(It.IsAny<string>()), Times.Never);

    }

    // EDGE CASE — whitespace IconPublicId is treated as "no icon"
    [Fact]
    public async Task UploadIconAsync_DoesNotCallDelete_WhenOldPublicIdIsWhitespace()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id, iconPublicId: "   ");

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _cloudinary
            .Setup(c => c.UpdateAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(new ImageUpdatedResponse("https://new.com/icon.png", "new_id"));
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.UploadIconAsync(id, CreateFormFile());

        _cloudinary.Verify(c => c.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    // EDGE CASE — Cloudinary folder path encodes the category id correctly
    [Fact]
    public async Task UploadIconAsync_BuildsCorrectFolderPath_ForCloudinary()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id);
        string? capturedFolder = null;

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _cloudinary
            .Setup(c => c.UpdateAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .Callback<IFormFile, string>((_, folder) => capturedFolder = folder)
            .ReturnsAsync(new ImageUpdatedResponse("https://url.com", "pub_id"));
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.UploadIconAsync(id, CreateFormFile());

        Assert.Equal($"categories/{id}", capturedFolder);
    }

    // GUARD — category not found throws NotFoundException before touching Cloudinary
    [Fact]
    public async Task UploadIconAsync_ThrowsNotFound_WhenCategoryDoesNotExist()
    {
        var id = Guid.NewGuid();
        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UploadIconAsync(id, CreateFormFile()));

        _cloudinary.Verify(c => c.UpdateAsync(
            It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // GUARD — validation fails → Cloudinary and repo never reached
    [Fact]
    public async Task UploadIconAsync_ThrowsAndTouchesNothing_WhenValidationFails()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id);

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _imageValidator
            .Setup(v => v.Validate(It.IsAny<IFormFile>()))
            .Throws(new InvalidOperationException("Invalid file type."));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UploadIconAsync(id, CreateFormFile("malware.exe")));

        _cloudinary.Verify(c => c.UpdateAsync(
            It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // EDGE CASE — UpdatedAt is set to a recent UtcNow value after upload
    [Fact]
    public async Task UploadIconAsync_SetsUpdatedAt_ToUtcNow()
    {
        var id = Guid.NewGuid();
        var category = CreateCategory(id: id);
        var before = DateTime.UtcNow.AddSeconds(-1);

        _categoryRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);
        _cloudinary
            .Setup(c => c.UpdateAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(new ImageUpdatedResponse("https://url.com", "pub_id"));
        _unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.UploadIconAsync(id, CreateFormFile());

        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.NotNull(category.UpdatedAt);
        Assert.InRange(category.UpdatedAt!.Value, before, after);
    }
}