using System.Text.Json.Serialization;
using Hlt.Api;
using Hlt.Api.Auth;
using Hlt.Application;
using Hlt.Application.Abstractions;
using Hlt.Infrastructure;
using Hlt.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- 1. Tham số nghiệp vụ (BR-08) ----------
var rules = config.GetSection("BookingRules").Get<BookingRules>() ?? new BookingRules();

// ---------- 2. Đăng ký các lớp (Dependency Injection) ----------
builder.Services.AddApplication(rules);           // Hlt.Api/ServiceRegistration.cs
builder.Services.AddInfrastructure(config);       // Hlt.Infrastructure/DependencyInjection.cs

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<IClaimsTransformation, AppUserClaimsTransformation>();

// ---------- 3. Xác thực OpenID Connect (token JWT do Keycloak cấp) ----------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var oidc = config.GetSection("Oidc");
        o.Authority = oidc["Authority"];                 // http://localhost:8080/realms/hlt01
        o.Audience = oidc["Audience"];                   // hlt-web
        o.RequireHttpsMetadata = oidc.GetValue("RequireHttpsMetadata", false);
        o.MapInboundClaims = false;                      // giữ nguyên tên claim "sub", "email", "name"
        o.TokenValidationParameters.NameClaimType = "name";
        o.TokenValidationParameters.RoleClaimType = "role";

        // Khi API chạy trong Docker: lấy cấu hình từ http://keycloak:8080 nhưng token vẫn do localhost:8080 cấp
        if (!string.IsNullOrWhiteSpace(oidc["MetadataAddress"])) o.MetadataAddress = oidc["MetadataAddress"]!;
        if (!string.IsNullOrWhiteSpace(oidc["ValidIssuer"])) o.TokenValidationParameters.ValidIssuer = oidc["ValidIssuer"];
    });
builder.Services.AddAuthorization();

// ---------- 4. Controllers, CORS, xử lý lỗi ----------
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(config.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => "HLT01 API đang chạy");

// ---------- 5. Tạo/cập nhật CSDL SQL Server và dữ liệu mẫu ----------
await app.Services.InitialiseDatabaseAsync(seed: config.GetValue("SeedData", true));

app.Run();