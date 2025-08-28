using System.Security.Claims;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(opt => opt.UseSqlite("Data Source=taskapi.db"));
builder.Services.AddScoped<JwtService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            //connect to React frontend  
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

//Auth
var key = builder.Configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT__KEY") ?? "mysuperlongrandomsecretkey_must_be_32chars_or_longer!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TaskApi",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TaskApiClient",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

//api routes
app.MapPost("/register", async (RegisterDto dto, AppDb db) =>
{
    dto = dto with { Username = dto.Username.Trim() };
    if (dto.Username.Length < 3 || dto.Password.Length < 6)
        return Results.BadRequest(new { error = "Username >= 3, Password >= 6" });

    if (await db.Users.AnyAsync(u => u.Username == dto.Username))
        return Results.Conflict(new { error = "Username already exists" });

    var user = new User
    {
        Username = dto.Username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", new { user.Id, user.Username });
});

app.MapPost("/login", async (LoginDto dto, AppDb db, JwtService jwt) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
    if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        return Results.Unauthorized();

    var token = jwt.Generate(user);
    return Results.Ok(new { token });
});

app.MapGet("/me", async (ClaimsPrincipal user, AppDb db) =>
{
    var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (idStr is null) return Results.Unauthorized();
    var id = int.Parse(idStr);
    var me = await db.Users.Where(u => u.Id == id).Select(u => new { u.Id, u.Username }).FirstAsync();
    return Results.Ok(me);
}).RequireAuthorization();


app.MapGet("/todos", async (ClaimsPrincipal user, AppDb db) =>
{
    var id = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var todos = await db.Todos.Where(t => t.UserId == id)
                              .OrderBy(t => t.Done).ThenBy(t => t.Id)
                              .Select(t => new { t.Id, t.Title, t.Done })
                              .ToListAsync();
    return Results.Ok(todos);
}).RequireAuthorization();

app.MapPost("/todos", async (ClaimsPrincipal user, TodoCreateDto dto, AppDb db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Title)) return Results.BadRequest(new { error = "Title required" });
    var id = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var todo = new Todo { Title = dto.Title.Trim(), Done = dto.Done, UserId = id };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{todo.Id}", new { todo.Id, todo.Title, todo.Done });
}).RequireAuthorization();

app.MapPut("/todos/{id:int}", async (ClaimsPrincipal user, int id, TodoUpdateDto dto, AppDb db) =>
{
    var uid = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid);
    if (todo is null) return Results.NotFound();

    if (!string.IsNullOrWhiteSpace(dto.Title)) todo.Title = dto.Title.Trim();
    todo.Done = dto.Done;
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/todos/{id:int}", async (ClaimsPrincipal user, int id, AppDb db) =>
{
    var uid = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid);
    if (todo is null) return Results.NotFound();

    db.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();


app.Run();