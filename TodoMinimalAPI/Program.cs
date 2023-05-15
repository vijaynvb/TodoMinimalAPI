using Microsoft.EntityFrameworkCore;
using TodoMinimalAPI.Data;
using TodoMinimalAPI.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TodoDBContext>(opt => opt.UseInMemoryDatabase("TodoDB"));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/todos", async (TodoDBContext db) => await db.Todos.ToListAsync());

app.MapGet("/todos/{id}", async (TodoDBContext db, int id) =>
            await db.Todos.FindAsync(id)
                is Todo todo ? Results.Ok(todo) : Results.NotFound());

app.MapPost("/todos", async (TodoDBContext db, Todo todo) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{todo.Id}", todo);
});

app.MapPut("/todos/{id}", async (TodoDBContext db, Todo todo, int id) =>
{
    var oldTodo = await db.Todos.FindAsync(id);
    if(todo is null) return Results.NotFound();
    // automapper
    oldTodo.Title = todo.Title;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/todos/{id}", async (TodoDBContext db, int id) =>
{
    if( await db.Todos.FindAsync(id) is Todo todo )
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.Ok(todo);
    }
    return Results.NotFound();
});

app.Run();

