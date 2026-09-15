using Formify.Api.Exceptions;
using Formify.Api.Models;
using interfaces;
using Formify.Api;
using Formify.Api.Dtos;
using Formify.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

namespace Extensions;

public static class Extesnsions
{

    public async static Task AssertFolderOwnershipAsync(this Guid id, Guid CurrentUserId, IProjectService projectSerivce,
        IFolderSerivce folderSerivce, CancellationToken ct = default)
    {
        Folder folder = await folderSerivce.GetFolderById(id, ct)
            ?? throw new NotFoundException("Folder not found");
        var project = await projectSerivce.GetProjectAsync(folder.ProjectId, CurrentUserId, ct);
        if (project is null || project.OwnerId != CurrentUserId) throw new ForbiddenException();
    }

    public async static Task<AppFile> GetFileWithOwnershipAsync(this Guid fileId, Guid CurrentUserId, IProjectService projectSerivce,
        IFolderSerivce folderSerivce, IFileService fileService, CancellationToken ct)
    {
        Console.WriteLine("inside extension", fileId);
        var file = await fileService.GetFileById(fileId, ct)
            ?? throw new NotFoundException("File not found");
        await fileId.AssertFolderOwnershipAsync(CurrentUserId, projectSerivce, folderSerivce, ct);
        return file;
    }

    public static AppFileDto ToDto(this AppFile file) => new(
     file.Id,
     file.Name,
     file.FolderId,
     file.SizeBytes,
     file.Metadata.Select(metadata => new AppFileMetadataDto(metadata.Key, metadata.Value)).ToList(),
     file.CreatedAt,
     file.UpdatedAt);

    public static FolderDto ToDto(this Folder f) =>
    new(f.Id, f.Name, f.ProjectId, f.CreatedAt, f.UpdatedAt, f.FileCount(), f.HasSchema());

    public static ProjectDto ToDto(this Project p) => new(p.Id, p.Name, p.OwnerId, p.CreatedAt, p.UpdatedAt, p.FolderCount());

    public static SchemaTemplateDto ToDto(this SchemaTemplate schema) => new(
    schema.Id,
    schema.FolderId,
    schema.EntrySchema,
    schema.Schemas.ToDictionary(
        s => s.Name,
        s => new SchemaEntryDto(s.SchemaYaml, s.TemplateHtml, s.TemplateCss)
    ),
    schema.CreatedAt,
    schema.UpdatedAt
);

    public static SchemaEntryDto ToDto(this SchemaEntry s)
    {
        return new SchemaEntryDto(s.SchemaYaml, s.TemplateHtml, s.TemplateCss);
    }

    public static IServiceCollection RegisterServices(this IServiceCollection serviceDescriptors)
    {
        // ─── Services ───────────────────────────────────────────────────────────────
        serviceDescriptors.AddScoped<IAuthService, AuthService>();
        serviceDescriptors.AddScoped<IFileContentService, FileContentService>();
        serviceDescriptors.AddScoped<IBreadcrumbService, BreadcrumbService>();
        serviceDescriptors.AddScoped<IProjectService, ProjectService>();
        serviceDescriptors.AddScoped<IFileService, FileService>();
        serviceDescriptors.AddScoped<IFolderSerivce, FolderService>();
        serviceDescriptors.AddScoped<ISchemaService, SchemaService>();
        serviceDescriptors.AddScoped<IMetaDataService, MetaDataService>();
        serviceDescriptors.AddScoped<ISchemaEntryService, SchemaEntryService>();
        serviceDescriptors.AddSingleton<IPdfExportService, PdfExportService>();

        return serviceDescriptors;
    }


    public static IServiceCollection RegisterCors(this IServiceCollection services, string myAllowSpecificOrigins)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(name: myAllowSpecificOrigins,
                policy =>
                {
                    policy.WithOrigins("http://localhost:5175")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .WithExposedHeaders("Content-Disposition");

                });
        });

        return services;
    }

    public static IServiceCollection RegisterAuthentication(this IServiceCollection serviceDescriptors, IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "Formify";
        var jwtAudience = configuration["Jwt:Audience"] ?? "Formify";

        serviceDescriptors.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep the "sub" claim name as-is
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                };
            });

        return serviceDescriptors;

    }


    public static IServiceCollection RegisterSwaggerUI(this IServiceCollection serviceDescriptors)
    {
        serviceDescriptors.AddSwaggerGen(c =>
       {
           c.SwaggerDoc("v1", new OpenApiInfo { Title = "Formify API", Version = "v1" });
           c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
           {
               Name = "Authorization",
               Type = SecuritySchemeType.Http,
               Scheme = "bearer",
               BearerFormat = "JWT",
               In = ParameterLocation.Header,
           });
           c.AddSecurityRequirement(new OpenApiSecurityRequirement
               {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            },
               });
       });

        return serviceDescriptors;
    }

    public static IServiceCollection RegisterCors(this IServiceCollection serviceDescriptors, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000" };

        serviceDescriptors.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials());
        });

        return serviceDescriptors;
    }




}