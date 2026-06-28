# Watchlist Module - Best Practices & Maintenance Guide

## Code Quality Standards

### 1. Architecture Principles
- **Separation of Concerns**: Models, Views, Controllers properly separated
- **Single Responsibility**: Each class has one reason to change
- **Dependency Injection**: Services injected via constructor
- **Async Operations**: Database calls are asynchronous

### 2. Database Best Practices

#### Connection Management
```csharp
// ? Good: Uses dependency injection
public WatchlistController(ApplicationDbContext context)
{
    _context = context;
}

// ? Good: Async operations for better performance
public async Task<IActionResult> Index()
{
    var watchlists = await _context.Watchlists.ToListAsync();
    return View(watchlists);
}
```

#### Data Validation
```csharp
// ? Good: Model validation with data annotations
[Required]
[StringLength(100)]
public string ScriptName { get; set; }

// ? Good: Database constraints in OnModelCreating
modelBuilder.Entity<Watchlist>(entity =>
{
    entity.Property(e => e.ScriptName).IsRequired().HasMaxLength(100);
});
```

### 3. Error Handling
```csharp
// ? Good: Comprehensive error handling
try
{
    watchlist.ModifiedDate = DateTime.Now;
    _context.Update(watchlist);
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogError(ex, "Concurrency error updating watchlist");
    ModelState.AddModelError("", "The record was modified by another user");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error updating watchlist");
    ModelState.AddModelError("", "An error occurred while updating");
}
```

### 4. Security Practices

#### CSRF Protection
```csharp
// ? Good: All POST/PUT/DELETE actions have CSRF token validation
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create([Bind("...")] Watchlist watchlist)
{
    // Implementation
}

<!-- ? Good: CSRF token in forms -->
<form asp-action="Create" method="post">
    <!-- Hidden CSRF token automatically added by asp-form-action-helper -->
</form>
```

#### Input Validation
```csharp
// ? Good: Server-side validation always performed
if (ModelState.IsValid)
{
    // Only process valid data
    _context.Add(watchlist);
    await _context.SaveChangesAsync();
}

// ? Avoid: Never trust client-side validation alone
```

### 5. Performance Optimization

#### Query Optimization
```csharp
// ? Good: Ordered at database level
var watchlists = await _context.Watchlists
    .OrderBy(w => w.GroupName)
    .ThenBy(w => w.ScriptName)
    .ToListAsync();

// ? Avoid: Ordering in application memory
var watchlists = _context.Watchlists.ToList()
    .OrderBy(w => w.GroupName) // Bad - loads all data first
    .ToList();
```

#### Async/Await
```csharp
// ? Good: Async operations throughout the stack
public async Task<IActionResult> Index()
{
    var watchlists = await _context.Watchlists.ToListAsync();
    return View(watchlists);
}

// ? Avoid: Blocking operations
public IActionResult Index()
{
    var watchlists = _context.Watchlists.ToList(); // Blocks thread
    return View(watchlists);
}
```

## Maintenance Guide

### Regular Maintenance Tasks

#### 1. Database Backup
```bash
# Backup SQLite database
copy zerodatrade.db zerodatrade_backup.db

# Or use automated backup
```

#### 2. Log Review
- Monitor application logs for errors
- Check for repeated exceptions
- Review performance metrics

#### 3. Code Updates
```bash
# Update NuGet packages
dotnet restore
dotnet outdated  # List outdated packages

# Update specific package
dotnet add package Microsoft.EntityFrameworkCore --version 9.0.0
```

#### 4. Security Updates
- Keep .NET framework updated
- Update NuGet packages regularly
- Review security advisories

### Database Maintenance

#### Adding New Fields (Migration)
```csharp
// 1. Add property to model
public class Watchlist
{
    // ...existing properties...
    public string? Category { get; set; } // New field
}

// 2. Update DbContext if needed
// 3. Create migration
dotnet ef migrations add AddCategoryToWatchlist

// 4. Update database
dotnet ef database update
```

#### Clearing Data
```csharp
// In DbInitializer or as needed
var context = new ApplicationDbContext();
context.Database.EnsureDeleted();  // Delete database
context.Database.EnsureCreated();  // Recreate
```

## Troubleshooting Guide

### Problem: "There is already an open DataReader"
**Cause**: Nested asynchronous operations on same context
**Solution**:
```csharp
// ? Fix: Complete query before using results in another query
var groups = await _context.Watchlists
    .GroupBy(w => w.GroupName)
    .ToListAsync();
```

### Problem: "DbUpdateConcurrencyException"
**Cause**: Record modified by another user simultaneously
**Solution**:
```csharp
catch (DbUpdateConcurrencyException ex)
{
    // Reload the entity from database
    await ex.Entries[0].ReloadAsync();
    // Show user message to retry
}
```

### Problem: "Navigation property was not loaded"
**Cause**: Lazy loading not enabled or related data not included
**Solution**:
```csharp
// Use Include for related data
var watchlist = await _context.Watchlists
    .Include(w => w.RelatedEntity)
    .FirstOrDefaultAsync();
```

### Problem: "No database provider configured"
**Cause**: DbContext not configured in dependency injection
**Solution**:
```csharp
// In Program.cs, ensure this line exists:
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
```

## Performance Tuning

### Query Optimization Checklist
- [ ] All database queries are async
- [ ] Queries include necessary `.Where()` clauses
- [ ] `.ToList()` called only when necessary
- [ ] Large result sets are paged
- [ ] Indexes exist on frequently searched columns
- [ ] N+1 query problems resolved with `.Include()`

### Caching Strategy (Future Enhancement)
```csharp
// Example: Cache frequently accessed data
services.AddMemoryCache();

// In controller:
public async Task<IActionResult> Index()
{
    if (!_cache.TryGetValue("watchlists", out var watchlists))
    {
        watchlists = await _context.Watchlists.ToListAsync();
        _cache.Set("watchlists", watchlists, TimeSpan.FromHours(1));
    }
    return View(watchlists);
}
```

## Testing Strategy

### Unit Testing Template
```csharp
[TestClass]
public class WatchlistControllerTests
{
    private Mock<ApplicationDbContext> _mockContext;
    private WatchlistController _controller;

    [TestInitialize]
    public void Setup()
    {
        _mockContext = new Mock<ApplicationDbContext>();
        _controller = new WatchlistController(_mockContext.Object, _logger);
    }

    [TestMethod]
    public async Task Index_ReturnsViewWithWatchlists()
    {
        // Arrange
        var watchlists = new List<Watchlist> { /* test data */ };
        
        // Act
        var result = await _controller.Index();
        
        // Assert
        Assert.IsNotNull(result);
    }
}
```

## Monitoring & Logging

### Key Metrics to Monitor
- Response time of CRUD operations
- Database connection failures
- Validation errors frequency
- Exception rates

### Logging Best Practices
```csharp
// ? Good: Structured logging with context
_logger.LogInformation("Creating watchlist entry: {@Watchlist}", watchlist);
_logger.LogError(ex, "Error creating watchlist: ScriptName={ScriptName}", 
    watchlist.ScriptName);

// ? Avoid: String concatenation
_logger.LogError("Error: " + ex.Message);
```

## Deployment Checklist

- [ ] All tests pass
- [ ] Code review completed
- [ ] Security scan passed
- [ ] No NuGet vulnerabilities
- [ ] Database backups created
- [ ] Connection strings configured
- [ ] Logging configured for production
- [ ] HTTPS enabled
- [ ] CORS policies configured if needed
- [ ] Rate limiting implemented if needed

## Version Control Best Practices

### Commit Strategy
```bash
# Meaningful commit messages
git commit -m "feat: Add filtering to watchlist index"
git commit -m "fix: Resolve concurrent edit issue in watchlist edit"
git commit -m "docs: Update watchlist module documentation"
```

### Branch Strategy
```bash
main                    # Production
??? develop             # Development
??? feature/watchlist   # Feature branches
??? fix/concurrency     # Bug fixes
??? docs/readme         # Documentation
```

## Documentation Standards

### Code Comments
```csharp
// ? Good: Explain WHY, not WHAT
// Group watchlist entries to improve UI readability
var groupedWatchlists = Model.GroupBy(w => w.GroupName);

// ? Avoid: Obvious comments
// Increment i
i++;
```

### Method Documentation
```csharp
/// <summary>
/// Retrieves all watchlist entries grouped by GroupName.
/// </summary>
/// <returns>IActionResult containing grouped watchlist view.</returns>
/// <exception cref="DbUpdateException">Thrown when database error occurs.</exception>
public async Task<IActionResult> Index()
{
    // Implementation
}
```

## Scaling Considerations

### When to Refactor
1. **Query Performance**: Add indexes, implement caching
2. **Data Volume**: Implement pagination, soft deletes
3. **Concurrent Users**: Add connection pooling, optimize queries
4. **Feature Complexity**: Refactor into services/repositories

### Example: Repository Pattern
```csharp
public interface IWatchlistRepository
{
    Task<IEnumerable<Watchlist>> GetAllAsync();
    Task<Watchlist> GetByIdAsync(int id);
    Task AddAsync(Watchlist watchlist);
    // ... other methods
}

// Implementation
public class WatchlistRepository : IWatchlistRepository
{
    private readonly ApplicationDbContext _context;
    
    public async Task<IEnumerable<Watchlist>> GetAllAsync()
    {
        return await _context.Watchlists
            .OrderBy(w => w.GroupName)
            .ToListAsync();
    }
}
```

## Summary

? **Current Best Practices Implemented**:
- Async/await throughout
- Proper error handling
- CSRF protection
- Input validation
- Dependency injection
- Separation of concerns

?? **Future Improvements**:
- Unit tests
- Integration tests
- Caching layer
- Repository pattern
- Automated backups
- Advanced logging
- Performance monitoring

---

**Document Version**: 1.0
**Last Updated**: 2024
**Maintenance**: Quarterly review recommended
