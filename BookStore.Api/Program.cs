using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<BooksContext>(options => 
    options.UseSqlite(builder.Configuration.GetConnectionString("BooksDb")));

var app = builder.Build();
app.UseSwagger().UseSwaggerUI();
app.UseHttpsRedirection();

app.MapBookEndpoints();

app.Run();

class Book
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int LaunchYear { get; set; }
    public decimal Price { get; set; }
}

class BooksContext : DbContext
{
    public BooksContext(DbContextOptions<BooksContext> options) : base(options)  => Database.EnsureCreated();
    
    public DbSet<Book> Books { get; set; } = default!;
}

static class BookEndpoints
{
    public static void MapBookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/Books")
            .WithTags(nameof(Book))
            .WithOpenApi();

        group.MapGet("/", async (BooksContext db) => await db.Books.ToListAsync())
            .WithName("GetAllBooks");

        group.MapGet("/{id:int}", async Task<Results<Ok<Book>, NotFound>> (int id, BooksContext db) =>
                await db.Books.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) is { } model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound())
            .WithName("GetBookById");

        group.MapPut("/{id:int}", async Task<Results<Ok, NotFound>> (int id, Book book, BooksContext db) =>
            {
                var affected = await db.Books
                    .Where(model => model.Id == id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(m => m.Title, book.Title)
                        .SetProperty(m => m.Description, book.Description)
                        .SetProperty(m => m.LaunchYear, book.LaunchYear)
                        .SetProperty(m => m.Price, book.Price));
                return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
            })
            .WithName("UpdateBook");

        group.MapPost("/", async (Book book, BooksContext db) =>
            {
                db.Books.Add(book);
                await db.SaveChangesAsync();
                return TypedResults.Created($"/api/Books/{book.Id}", book);
            })
            .WithName("CreateBook");

        group.MapDelete("/{id:int}", async Task<Results<Ok, NotFound>> (int id, BooksContext db) =>
                await db.Books.Where(model => model.Id == id).ExecuteDeleteAsync() == 1
                    ? TypedResults.Ok()
                    : TypedResults.NotFound())
            .WithName("DeleteBooks")
            .WithOpenApi();
    }
}
