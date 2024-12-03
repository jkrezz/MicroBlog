using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Blog;
using Blog.Services;
using Blog.Services.Interfaces;
using Microsoft.OpenApi.Models;
using Minio;

var builder = WebApplication.CreateBuilder(args);
// Настройка MinIO клиента
builder.Services.AddSingleton<IMinioClient>(provider =>
    new MinioClient()
        .WithEndpoint("127.0.0.1:9000")
        .WithCredentials("minioadmin", "minioadmin")
        .WithSSL(false)
        .Build());

// Add services to the container.
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IPostService, PostService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT токен в формате: Bearer {токен}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
    c.OperationFilter<FileUploadOperation>();
});

var jwtOptions = builder.Configuration
    .GetSection("JwtOptions")
    .Get<JwtOptions>();
    
builder.Services.AddSingleton(jwtOptions);

// Configuring the Authentication Service
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        //convert the string signing key to byte array
        byte[] signingKeyBytes = Encoding.UTF8
            .GetBytes(jwtOptions.SigningKey);

        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes)
        };
    });

// Configuring the Authorization Service
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "BlogService API v1"));

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

