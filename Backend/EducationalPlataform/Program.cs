using EducationalPlataform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi; 




var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();


// auto mapper configuration
builder.Services.AddAutoMapper(typeof(Program).Assembly);


// DbContext configuration (SQL Server Express)
builder.Services.AddDbContext<EducationalPlataformContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("EducationalPlataformContext") ?? throw new InvalidOperationException("Connection string 'EducationalPlataformContext' not found.")));

// Swagger/OpenAPI configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EducationalPlataform API",
        Version = "v1"
    });
});



var app = builder.Build();

// apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EducationalPlataformContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EducationalPlataform API V1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
