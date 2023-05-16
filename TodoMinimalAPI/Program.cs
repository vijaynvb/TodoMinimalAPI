using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using TodoMinimalAPI.Configurations;
using TodoMinimalAPI.CustomMiddleware;
using TodoMinimalAPI.Data;
using TodoMinimalAPI.DTO;
using TodoMinimalAPI.Model;
using TodoMinimalAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TodoDBContext>(opt => opt.UseInMemoryDatabase("TodoDB"));
builder.Services.AddAutoMapper(typeof(AutoMapperConfig));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<TodoDBContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredUniqueChars = 0;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
});

// configure the token decoding logic verify the token 
// algorithm to decode the token
var issuer = builder.Configuration["JWT:Issuer"];
var audience = builder.Configuration["JWT:Audience"];
var key = builder.Configuration["JWT:Key"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };
});

builder.Services.AddAuthorization(options => {
    options.AddPolicy("admin_greetings", policy => policy.RequireAuthenticatedUser());});

// policy who can access it 

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(opt =>
{
    opt.AddPolicy( name: MyAllowSpecificOrigins, policy =>
    {
        //policy.AllowAnyOrigin()
        policy.WithOrigins("https://localhost:44360", "mydomain.com")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

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

// [Authorize]
app.MapGet("/todos", async (TodoDBContext db) => Results.Ok(await db.Todos.ToListAsync()))
    .RequireAuthorization("admin_greetings");

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

app.MapPost("/signup", async (TodoDBContext db, IMapper mapper, UserManager <ApplicationUser> userManager, SignUpDTO userDTO) =>
{
    var user = mapper.Map<ApplicationUser>(userDTO);

    var newUser = await userManager.CreateAsync(user, userDTO.Password);
    if (newUser.Succeeded)
        return user;
    return null;
});

app.MapPost("/login", async (TodoDBContext db, 
                            SignInManager < ApplicationUser > signInManager,
                            UserManager < ApplicationUser > userManager,
                            IConfiguration appConfig,
                            LoginDTO loginDTO) =>
{
    // generate a token and return a token
    var issuer = appConfig["JWT:Issuer"];
    var audience = appConfig["JWT:Audience"];
    var key = appConfig["JWT:Key"];

    if (loginDTO is not null)
    {
        var loginResult = await signInManager.PasswordSignInAsync(loginDTO.UserName, loginDTO.Password, loginDTO.RememberMe, false);
        if (loginResult.Succeeded)
        {
            // generate a token
            var user = await userManager.FindByEmailAsync(loginDTO.UserName);
            if (user != null)
            {
                var keyBytes = Encoding.UTF8.GetBytes(key);
                var theKey = new SymmetricSecurityKey(keyBytes); // 256 bits of key
                var creds = new SigningCredentials(theKey, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken(issuer, audience, null, expires: DateTime.Now.AddMinutes(30), signingCredentials: creds);
                return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) }); // token 
            }
        }
    }
    return Results.BadRequest();
});

app.UseCors(MyAllowSpecificOrigins);
// has a valid api key if it is valid then allow to access the endpoint if not deny
app.UseMiddleware<ApiKeyAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.Run();

