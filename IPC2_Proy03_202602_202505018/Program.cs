
using IPC2_Proy03_202602_202505018.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Configuración de controladores y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Servicios del sistema
builder.Services.AddSingleton<ValidadorNIT>();
builder.Services.AddScoped<RTUService>();
builder.Services.AddScoped<AutorizacionService>();

// Servicio para los reportes
builder.Services.AddScoped<ReporteService>();

var app = builder.Build();

// Swagger en modo desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
