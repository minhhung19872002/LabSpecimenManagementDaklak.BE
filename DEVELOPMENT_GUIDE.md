# Lab Specimen Management - Development Guide

## Mục lục
1. [Kiến trúc dự án](#1-kiến-trúc-dự-án)
2. [Quy tắc chung](#2-quy-tắc-chung)
3. [Thêm chức năng mới](#3-thêm-chức-năng-mới)
4. [Các service dùng chung](#4-các-service-dùng-chung)
5. [Extension methods](#5-extension-methods)
6. [Constants](#6-constants)
7. [Exception handling](#7-exception-handling)
8. [Mẫu code tham khảo](#8-mẫu-code-tham-khảo)

---

## 1. Kiến trúc dự án

Dự án sử dụng **ABP Framework** với kiến trúc **DDD (Domain-Driven Design)**.

```
src/
├── LabSpecimenManagement.Domain.Shared/     # Interfaces, Enums, Constants dùng chung
├── LabSpecimenManagement.Domain/            # Entities, Domain Services
├── LabSpecimenManagement.Application.Contracts/  # DTOs, Interfaces AppService
├── LabSpecimenManagement.Application/       # AppService implementations
├── LabSpecimenManagement.EntityFrameworkCore/    # DbContext, Repositories, Migrations
├── LabSpecimenManagement.HttpApi/           # Controllers (nếu cần custom)
└── LabSpecimenManagement.HttpApi.Host/      # Host, Startup, Filters
```

### Các layer và trách nhiệm:

| Layer | Trách nhiệm |
|-------|-------------|
| `Domain.Shared` | Interfaces (`IHasActive`), Enums, Exceptions base |
| `Domain` | Entities (kế thừa `FullAuditedAggregateRoot<Guid>`) |
| `Application.Contracts` | DTOs, Interfaces (`I*AppService`) |
| `Application` | Business logic, Services, Extensions |
| `EntityFrameworkCore` | EF configurations, Migrations |
| `HttpApi` | Custom controllers (nếu cần) |
| `HttpApi.Host` | Filters, Middleware, Startup |

---

## 2. Quy tắc chung

### 2.1 Naming Conventions

| Loại | Convention | Ví dụ |
|------|------------|-------|
| Entity | PascalCase, số ít | `Laboratory`, `TestRequest` |
| DTO | Entity + `Dto` suffix | `LaboratoryDto` |
| Create/Update DTO | `CreateUpdate` + Entity + `Dto` | `CreateUpdateLaboratoryDto` |
| AppService Interface | `I` + Entity + `AppService` | `ILaboratoryAppService` |
| AppService | Entity + `AppService` | `LaboratoryAppService` |
| Repository | `I` + Entity + `Repository` | `ILaboratoryRepository` |

### 2.2 Không dùng Magic Strings/Numbers

**SAI:**
```csharp
if (CurrentUser.IsInRole("admin")) { ... }
var sorting = "CreationTime DESC";
var path = "templates";
```

**ĐÚNG:**
```csharp
using LabSpecimenManagement.Common;

if (CurrentUser.IsInRole(AppConstants.RoleAdmin)) { ... }
var sorting = AppConstants.DefaultEmployeeSorting;
var path = AppConstants.FolderTemplates;
```

### 2.3 Entity phải implement interface phù hợp

```csharp
// Entity có trạng thái active
public class Laboratory : FullAuditedAggregateRoot<Guid>, IHasActive
{
    public bool IsActive { get; set; }
}

// Entity có Code
public class Department : FullAuditedAggregateRoot<Guid>, IHasActive, IHasCode
{
    public string Code { get; set; }
}
```

---

## 3. Thêm chức năng mới

### 3.1 Thêm Entity mới (VD: `Product`)

#### Bước 1: Tạo Entity (`Domain/Products/Product.cs`)

```csharp
using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Products;

public class Product : FullAuditedAggregateRoot<Guid>, IHasActive, IHasCode, IHasName
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }

    protected Product() { }

    public Product(Guid id, string code, string name, decimal price) : base(id)
    {
        Code = code;
        Name = name;
        Price = price;
        IsActive = true;
    }
}
```

#### Bước 2: Tạo DTOs (`Application.Contracts/Products/`)

**ProductDto.cs:**
```csharp
using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Products;

public class ProductDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}
```

**CreateUpdateProductDto.cs:**
```csharp
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Products;

public class CreateUpdateProductDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;
}
```

#### Bước 3: Tạo Interface (`Application.Contracts/Products/IProductAppService.cs`)

```csharp
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Products;

public interface IProductAppService : ICrudAppService<
    ProductDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateProductDto>
{
    Task<ListResultDto<ProductDto>> GetActiveProductsAsync();
    Task<ListResultDto<ProductDto>> GetAllAsync();
}
```

#### Bước 4: Tạo AppService (`Application/Products/ProductAppService.cs`)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Products;

[RemoteService(IsEnabled = false)]  // ABP auto-generates API từ interface
public class ProductAppService :
    CrudAppService<
        Product,
        ProductDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateProductDto>,
    IProductAppService
{
    private readonly IEntityValidationService _validationService;

    public ProductAppService(
        IRepository<Product, Guid> repository,
        IEntityValidationService validationService)
        : base(repository)
    {
        _validationService = validationService;
    }

    public async Task<ListResultDto<ProductDto>> GetActiveProductsAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .OrderBy(x => x.Name)
            .ToList();

        var dtos = ObjectMapper.Map<List<Product>, List<ProductDto>>(items);
        return new ListResultDto<ProductDto>(dtos);
    }

    public async Task<ListResultDto<ProductDto>> GetAllAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query.OrderBy(x => x.Name).ToList();

        var dtos = ObjectMapper.Map<List<Product>, List<ProductDto>>(items);
        return new ListResultDto<ProductDto>(dtos);
    }

    public override async Task<ProductDto> CreateAsync(CreateUpdateProductDto input)
    {
        // Validate unique code
        await _validationService.ValidateUniqueCodeAsync(
            Repository, input.Code, "Sản phẩm");

        // Validate unique name
        await _validationService.ValidateUniqueNameAsync(
            Repository, input.Name, "Sản phẩm");

        return await base.CreateAsync(input);
    }

    public override async Task<ProductDto> UpdateAsync(Guid id, CreateUpdateProductDto input)
    {
        await _validationService.ValidateUniqueCodeAsync(
            Repository, input.Code, "Sản phẩm", excludeId: id);

        await _validationService.ValidateUniqueNameAsync(
            Repository, input.Name, "Sản phẩm", excludeId: id);

        return await base.UpdateAsync(id, input);
    }
}
```

#### Bước 5: Thêm AutoMapper mapping

Trong `Application/LabSpecimenManagementApplicationAutoMapperProfile.cs`:

```csharp
// Product mappings
CreateMap<Product, ProductDto>();
CreateMap<CreateUpdateProductDto, Product>();
```

#### Bước 6: Cấu hình EF Core

Trong `EntityFrameworkCore/LabSpecimenManagementDbContext.cs`:

```csharp
public DbSet<Product> Products { get; set; }
```

Trong `EntityFrameworkCore/LabSpecimenManagementDbContextModelCreatingExtensions.cs`:

```csharp
builder.Entity<Product>(b =>
{
    b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Products",
              LabSpecimenManagementConsts.DbSchema);
    b.ConfigureByConvention();

    b.Property(x => x.Code).IsRequired().HasMaxLength(50);
    b.Property(x => x.Name).IsRequired().HasMaxLength(200);
    b.Property(x => x.Description).HasMaxLength(500);

    b.HasIndex(x => x.Code).IsUnique();
});
```

#### Bước 7: Tạo Migration

```bash
dotnet ef migrations add AddProduct -p src/LabSpecimenManagement.EntityFrameworkCore -s src/LabSpecimenManagement.HttpApi.Host
dotnet ef database update -p src/LabSpecimenManagement.EntityFrameworkCore -s src/LabSpecimenManagement.HttpApi.Host
```

---

## 4. Các service dùng chung

### 4.1 `IEntityValidationService`

**File:** `Application/Common/Services/EntityValidationService.cs`

Dùng để validate unique cho entities.

```csharp
// Validate unique với predicate tùy chỉnh
await _validationService.ValidateUniqueAsync(
    Repository,
    x => x.Code == input.Code,
    "code",
    $"Mã '{input.Code}' đã tồn tại",
    excludeId: id);  // null nếu create, id nếu update

// Validate unique code (shorthand)
await _validationService.ValidateUniqueCodeAsync(
    Repository, input.Code, "Khoa phòng", excludeId: id);

// Validate unique name (shorthand)
await _validationService.ValidateUniqueNameAsync(
    Repository, input.Name, "Khoa phòng", excludeId: id);
```

### 4.2 `IUserNameProvider`

**File:** `Application/Common/Services/UserNameProvider.cs`

Dùng để lấy tên user từ user ID.

```csharp
// Lấy tên một user
string? name = await _userNameProvider.GetUserNameAsync(userId);

// Lấy tên nhiều users (batch)
var userNames = await _userNameProvider.GetUserNamesAsync(userIds);

// Enrich DTOs với user names
await _userNameProvider.EnrichWithUserNamesAsync(
    dtos,
    dto => dto.CreatedById,
    (dto, name) => dto.CreatedByName = name);
```

### 4.3 `ICodeGeneratorService`

**File:** `Application/Common/Services/CodeGeneratorService.cs`

Dùng để generate code tự động.

```csharp
// Generate code với date: PREFIX-YYYYMMDD-0001
string code = _codeGenerator.GenerateCodeWithDate("SPE", existingCount);
// Result: SPE-20260101-0001

// Generate code đơn giản: PREFIX-0001
string code = _codeGenerator.GenerateCode("CUST", existingCount);
// Result: CUST-0001

// Lấy prefix ngày hôm nay
string prefix = _codeGenerator.GetTodayPrefix("SPE");
// Result: SPE-20260101
```

---

## 5. Extension methods

### 5.1 `QueryableExtensions`

**File:** `Application/Common/Extensions/QueryableExtensions.cs`

```csharp
using LabSpecimenManagement.Common;

// Filter active records
var activeItems = query.WhereActive();

// Apply paging
var pagedItems = query.ApplyPaging(input);  // input: IPagedResultRequest

// Apply sorting với default
var sortedItems = query.ApplySorting(
    input.Sorting,
    x => x.CreationTime,
    defaultDescending: true);

// Apply sorting và paging
var result = query.ApplySortingAndPaging(
    input,
    x => x.Name,
    defaultDescending: false);

// Order by + ThenBy
var orderedItems = query.OrderByThenBy(
    x => x.SortOrder,
    x => x.Name);
```

### 5.2 `SearchFilterExtensions`

**File:** `Application/Common/Extensions/SearchFilterExtensions.cs`

```csharp
using LabSpecimenManagement.Common;

// Case-insensitive contains
bool matches = "Hello World".ContainsIgnoreCase("hello");  // true

// Match any of multiple fields
bool found = SearchFilterExtensions.MatchesAny(
    searchTerm,
    item.Code,
    item.Name,
    item.Description);

// Normalize search string
string? normalized = searchTerm.NormalizeSearch();  // trim + lowercase
```

---

## 6. Constants

### 6.1 `AppConstants`

**File:** `Application/Common/Constants/AppConstants.cs`

```csharp
using LabSpecimenManagement.Common;

// Role names
AppConstants.RoleAdmin                  // "admin"

// Folder paths
AppConstants.FolderTemplates           // "templates"
AppConstants.FolderUploads             // "uploads"
AppConstants.FolderTemp                // "temp"
AppConstants.FolderTestingImages       // "testing-images"

// Sorting
AppConstants.SortDescending            // "DESC"
AppConstants.SortAscending             // "ASC"
AppConstants.DefaultEmployeeSorting    // "CreationTime DESC"

// Property names (for ExtraProperties)
AppConstants.PropertyDepartmentId      // "DepartmentId"
AppConstants.PropertyPosition          // "Position"
AppConstants.PropertyLastLoginTime     // "LastLoginTime"
```

### 6.2 `SortingFields`

**File:** `Application/Common/Constants/SortingFields.cs`

```csharp
using LabSpecimenManagement.Common;

// Sử dụng trong switch expression cho dynamic sorting
switch (sorting?.ToLower())
{
    case SortingFields.TransferDate:
        query = query.OrderBy(x => x.TransferDate);
        break;
    case SortingFields.TransferDateDesc:
        query = query.OrderByDescending(x => x.TransferDate);
        break;
    case SortingFields.Status:
        query = query.OrderBy(x => x.Status);
        break;
    // ...
}
```

### 6.3 `TemplateConstants`

**File:** `Application/Common/Constants/TemplateConstants.cs`

```csharp
using LabSpecimenManagement.Common;

// Template filenames
TemplateConstants.TemplateBM01         // Form trả KQ
TemplateConstants.TemplateBM02         // Form trả KQ tổng hợp
TemplateConstants.TemplateBM03         // Form trả KQ đơn lẻ

// Checkbox characters
TemplateConstants.CheckedBox           // "☑"
TemplateConstants.UncheckedBox         // "☐"
TemplateConstants.GetCheckbox(true)    // "☑"

// Template helpers
TemplateConstants.GetDisplayName(filename)
TemplateConstants.GetTemplateFileName(name)
```

---

## 7. Exception handling

### 7.1 Các Exception types

**File:** `Domain.Shared/Common/Exceptions/BusinessException.cs`

```csharp
using LabSpecimenManagement.Common.Exceptions;

// Validation error
throw new ValidationException("code", "Mã không hợp lệ");
throw new ValidationException(new Dictionary<string, List<string>>
{
    ["code"] = new List<string> { "Mã không hợp lệ" },
    ["name"] = new List<string> { "Tên không được để trống" }
});

// Entity not found
throw new EntityNotFoundException("Khoa phòng", laboratoryId);

// Duplicate entity
throw new DuplicateEntityException("code", "LAB001");

// Forbidden access
throw new ForbiddenAccessException("Chỉ QLKT mới có quyền thêm người hỗ trợ");

// Unauthorized
throw new UnauthorizedAccessException("Bạn cần đăng nhập để thực hiện thao tác này");

// Invalid operation
throw new InvalidOperationException("Không thể xóa khoa phòng đang có công việc");
```

### 7.2 Exception Filter

**File:** `HttpApi.Host/Filters/ApiExceptionFilter.cs`

Exception Filter tự động chuyển đổi exceptions thành API response format:

```json
{
  "data": null,
  "success": false,
  "statusCode": 400,
  "errors": {
    "code": ["Mã 'LAB001' đã tồn tại"]
  }
}
```

---

## 8. Mẫu code tham khảo

### 8.1 AppService đầy đủ với search/filter/sort

```csharp
public async Task<PagedResultDto<ProductDto>> GetListAsync(GetProductListDto input)
{
    var query = await Repository.GetQueryableAsync();

    // Filter by active status
    if (input.IsActive.HasValue)
    {
        query = query.Where(x => x.IsActive == input.IsActive.Value);
    }

    // Search
    if (!string.IsNullOrWhiteSpace(input.Filter))
    {
        var search = input.Filter.NormalizeSearch();
        query = query.Where(x =>
            x.Code.ToLower().Contains(search!) ||
            x.Name.ToLower().Contains(search!));
    }

    // Count before paging
    var totalCount = query.Count();

    // Apply sorting and paging
    var items = query
        .ApplySortingAndPaging(input, x => x.Name)
        .ToList();

    var dtos = ObjectMapper.Map<List<Product>, List<ProductDto>>(items);

    return new PagedResultDto<ProductDto>(totalCount, dtos);
}
```

### 8.2 Kiểm tra quyền với role

```csharp
public async Task DeleteAsync(Guid id)
{
    var currentUserId = CurrentUser.Id;
    var isAdmin = CurrentUser.IsInRole(AppConstants.RoleAdmin);

    var entity = await Repository.GetAsync(id);

    // Chỉ admin hoặc owner mới được xóa
    if (!isAdmin && entity.CreatorId != currentUserId)
    {
        throw new ForbiddenAccessException("Bạn không có quyền xóa bản ghi này");
    }

    await Repository.DeleteAsync(entity);
}
```

### 8.3 Enrich DTOs với user names

```csharp
public async Task<PagedResultDto<TaskDto>> GetListAsync(GetTaskListDto input)
{
    // ... query logic ...

    var dtos = ObjectMapper.Map<List<Task>, List<TaskDto>>(items);

    // Enrich với tên người tạo
    await _userNameProvider.EnrichWithUserNamesAsync(
        dtos,
        dto => dto.CreatorId,
        (dto, name) => dto.CreatorName = name);

    // Enrich với tên người phân công
    await _userNameProvider.EnrichWithUserNamesAsync(
        dtos,
        dto => dto.AssigneeId,
        (dto, name) => dto.AssigneeName = name);

    return new PagedResultDto<TaskDto>(totalCount, dtos);
}
```

### 8.4 Partial class cho service lớn

Khi service quá dài (>500 dòng), tách thành partial classes:

```csharp
// LaboratoryTaskAppService.cs - Main file
public partial class LaboratoryTaskAppService : ApplicationService, ILaboratoryTaskAppService
{
    private readonly IRepository<LaboratoryTask, Guid> _repository;
    // ... dependencies ...

    // Core CRUD methods
}

// LaboratoryTaskAppService.Supporters.cs
public partial class LaboratoryTaskAppService
{
    // Supporter management methods
    public async Task<List<SupporterDto>> GetSupportersAsync(Guid taskId) { ... }
    public async Task AddSupporterAsync(Guid taskId, Guid userId) { ... }
}

// LaboratoryTaskAppService.Workflow.cs
public partial class LaboratoryTaskAppService
{
    // Workflow methods
    public async Task ApproveAsync(Guid taskId) { ... }
    public async Task RejectAsync(Guid taskId, string reason) { ... }
}
```

---

## Checklist khi thêm chức năng mới

- [ ] Entity implement đúng interfaces (`IHasActive`, `IHasCode`, `IHasName`)
- [ ] DTOs đặt đúng thư mục `Application.Contracts`
- [ ] Interface `I*AppService` kế thừa `ICrudAppService`
- [ ] AppService có attribute `[RemoteService(IsEnabled = false)]`
- [ ] Sử dụng `IEntityValidationService` cho validation
- [ ] Sử dụng `IUserNameProvider` cho user name lookup
- [ ] Sử dụng extension methods (`WhereActive()`, `ApplyPaging()`, etc.)
- [ ] Không có magic strings (dùng `AppConstants`, `SortingFields`, etc.)
- [ ] AutoMapper mapping được thêm trong Profile
- [ ] EF configuration trong DbContext
- [ ] Migration được tạo và apply

---

*Cập nhật lần cuối: 2026-01-01*
