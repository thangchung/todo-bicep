using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string corsName = "mycors";
builder.Services.AddCors(options => 
    options.AddPolicy(corsName, policy => { policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); }));

var connectionString = builder.Configuration.GetConnectionString("db");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<TodoDbContext>(options => 
    options.UseNpgsql(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly(typeof(TodoDbContext).Assembly.GetName().Name);
        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    })
    .UseSnakeCaseNamingConvention())
    .AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddSwaggerGen();

var app = builder.Build();

await EnsureDb(app.Services, app.Logger);

app.UseCors(corsName);

//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapFallback(() => Results.Redirect("/swagger"));

app.MapGet("/todos", async (TodoDbContext db) => await db.Todos.ToListAsync());

app.MapGet("/todos/{id}", async (TodoDbContext db, int id) =>
{
    return await db.Todos.FindAsync(id) switch
    {
        { } todo => Results.Ok(todo),
        null => Results.NotFound()
    };
});

app.MapPost("/todos", async (TodoDbContext db, Todo todo) =>
{
    await db.Todos.AddAsync(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todo/{todo.Id}", todo);
});

app.MapPut("/todos/{id}", async (TodoDbContext db, int id, Todo todo) =>
{
    if (id != todo.Id)
    {
        return Results.BadRequest();
    }

    if (!await db.Todos.AnyAsync(x => x.Id == id))
    {
        return Results.NotFound();
    }

    db.Update(todo);
    await db.SaveChangesAsync();

    return Results.Ok();
});


app.MapDelete("/todos/{id}", async (TodoDbContext db, int id) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null)
    {
        return Results.NotFound();
    }

    db.Todos.Remove(todo);
    await db.SaveChangesAsync();

    return Results.Ok();
});

app.Run();

async Task EnsureDb(IServiceProvider services, ILogger logger)
{
    logger.LogInformation("Ensuring database exists and is up to date at connection string '{ConnectionString}'", connectionString);

    await using var db = services.CreateScope().ServiceProvider.GetRequiredService<TodoDbContext>();
    await db.Database.MigrateAsync();
}

public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();
}

public class Todo
{
    public int Id { get; set; }
    [Required]
    public string? Title { get; set; }
    public bool IsComplete { get; set; }
}