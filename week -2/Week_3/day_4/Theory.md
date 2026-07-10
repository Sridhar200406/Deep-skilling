# Week 3 Day 4 — Theory: DTOs, AutoMapper, and Model Validation

## 1. Why DTOs (Data Transfer Objects)?

Without DTOs (Day 3), the API exposed the raw `Employee` entity directly:
- Client could see internal fields (navigation properties, EF Core metadata).
- Client could attempt to set `EmployeeId` on POST (security risk).
- Any change to the database schema broke the API contract instantly.
- Circular reference issues from navigation properties required workarounds.

With DTOs (Day 4):
- `EmployeeCreateDto` — only fields a client may set on creation. No `EmployeeId`.
- `EmployeeUpdateDto` — fields allowed on update. Includes `EmployeeId` for validation.
- `EmployeeReadDto` — what the client receives. Flattened, clean, computed properties.

```
Client ──POST──► EmployeeCreateDto ──AutoMapper──► Employee entity ──► DB
Client ◄──GET──  EmployeeReadDto   ◄─AutoMapper──  Employee entity ◄── DB
```

---

## 2. DTO Design Decisions

| DTO | Used In | EmployeeId? | Department? |
|-----|---------|-------------|-------------|
| `EmployeeCreateDto` | POST body | ❌ (DB assigns) | ❌ (only DepartmentId) |
| `EmployeeUpdateDto` | PUT body  | ✅ (route validation) | ❌ (only DepartmentId) |
| `EmployeeReadDto`   | GET response | ✅ | ✅ Flattened as DepartmentName |

`EmployeeReadDto` adds a `FullName` computed property (`FirstName + " " + LastName`)
that doesn't exist on the entity. AutoMapper's profile maps it.

---

## 3. AutoMapper — How It Works

AutoMapper eliminates manual property-by-property copying:

```csharp
// Without AutoMapper (tedious, error-prone):
var dto = new EmployeeReadDto
{
    EmployeeId   = employee.EmployeeId,
    FirstName    = employee.FirstName,
    LastName     = employee.LastName,
    FullName     = employee.FirstName + " " + employee.LastName,
    DepartmentName = employee.Department?.DepartmentName ?? ""
    // ... 8 more properties
};

// With AutoMapper (one line):
var dto = _mapper.Map<EmployeeReadDto>(employee);
```

### Profile Registration

```csharp
// EmployeeProfile.cs
public class EmployeeProfile : Profile
{
    public EmployeeProfile()
    {
        CreateMap<Employee, EmployeeReadDto>()
            .ForMember(d => d.FullName,
                opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.DepartmentName,
                opt => opt.MapFrom(s => s.Department != null
                    ? s.Department.DepartmentName : string.Empty));

        CreateMap<EmployeeCreateDto, Employee>();
        CreateMap<EmployeeUpdateDto, Employee>();
    }
}
```

### DI Registration

```csharp
// Program.cs — scans the assembly for all Profile subclasses
builder.Services.AddAutoMapper(typeof(EmployeeProfile).Assembly);
// Registers IMapper as Singleton automatically
```

### Injection & Usage

```csharp
public class EmployeeService
{
    private readonly IMapper _mapper;

    public EmployeeService(IEmployeeRepository repo, IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task<EmployeeReadDto> CreateEmployeeAsync(EmployeeCreateDto dto)
    {
        var entity = _mapper.Map<Employee>(dto);       // DTO → Entity
        await _repository.AddAsync(entity);
        return _mapper.Map<EmployeeReadDto>(entity);   // Entity → DTO
    }
}
```

---

## 4. Model Validation with Data Annotations

Data annotations on DTOs drive automatic `[ApiController]` validation:

```csharp
public class EmployeeCreateDto
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email must be valid.")]
    public string Email { get; set; } = string.Empty;

    [Range(0, 9999999.99)]
    public decimal Salary { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A valid DepartmentId is required.")]
    public int DepartmentId { get; set; }
}
```

When a request arrives with an invalid body, `[ApiController]` automatically returns:

```json
HTTP 400 Bad Request
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "FirstName": ["First name is required."],
    "Email": ["Email must be a valid email address."]
  }
}
```

No manual `if (!ModelState.IsValid)` check needed in the controller.

---

## 5. [StringLength] vs [MaxLength]

| Attribute | Used On | Enforced By |
|-----------|---------|-------------|
| `[MaxLength]` | Entity | EF Core schema (column size) |
| `[StringLength]` | DTO | ASP.NET Core model validation (API input) |

Both can be used together. DTOs use `[StringLength]` because it supports
`MinimumLength` and produces better validation error messages.

---

## 6. Full Data Flow — Day 4

```
POST /api/employees  { "FirstName": "David", ... }
    ↓
[ApiController] validates EmployeeCreateDto → 400 if invalid
    ↓
EmployeesController.CreateEmployee(createDto)
    ↓
EmployeeService.CreateEmployeeAsync(createDto)
    → _mapper.Map<Employee>(createDto)       ← DTO → Entity
    → _repository.AddAsync(entity)           ← INSERT to DB
    → _repository.LoadDepartmentAsync()      ← reload nav property
    → _mapper.Map<EmployeeReadDto>(entity)   ← Entity → DTO
    ↓
Controller returns 201 Created { EmployeeReadDto }
```

---

## 7. Architecture Summary — Day 4

```
EmployeesController   accepts/returns DTOs  (EmployeeCreateDto, EmployeeReadDto)
        ↓
EmployeeService       maps DTOs ↔ entities using IMapper
        ↓
EmployeeRepository    works with Employee entities only
        ↓
AppDbContext          EF Core → SQL Server
```

The Employee entity never leaves the service layer. The controller only ever sees DTOs.
