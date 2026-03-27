using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Experimental;
using MiniOA.Api.Filters;
using MiniOA.Api.Middleware;
using MiniOA.Infrastructure.Hubs;
using MiniOA.Core.Interfaces;
using MiniOA.Core.Mapping;
using MiniOA.Core.Models;
using MiniOA.Infrastructure;
using MiniOA.Infrastructure.Services;
using System.Text;

namespace MiniOA.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            /*=============================================================================================
            * 1. 配置数据库上下文
            * 2. 获取连接字符串
            * 3. 注册服务
            -----------------------------------------------------------------------------------------------
            */
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                        b => b.MigrationsAssembly("MiniOA.Infrastructure"));
                });
            /*
             * =============================================================================================
             */

            //注册AutoMapper
            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            
            // 配置跨域请求
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Any", policy =>
                {
                    policy.WithOrigins("http://localhost:5173", "https://familysys.hejiancheng.xyz").
                    AllowAnyHeader().
                    AllowAnyMethod().
                    AllowCredentials();
                });
            });

            /*=============================================================================================
             * 配置JWT认证
             */
            var jwtSections = builder.Configuration.GetSection("JwtSettings");
            builder.Services.Configure<JwtSettings>(jwtSections);

            var jwtSettings = jwtSections.Get<JwtSettings>();
            var key = Encoding.UTF8.GetBytes(jwtSettings!.SecretKey);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero //过期时间不允许有误差
                };

                // 为 SignalR 提供从 URL 查询字符串读取 Token 的能力
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/notificationHub")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
            //==============================================================================================

            //注册业务逻辑服务
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<IDepartmentService, DepartmentService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAuditLogService, AuditLogService>();
            builder.Services.AddScoped<IWorkflowService, WorkflowService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();

            

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //注册过滤器
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ValidationFilter>();
            });

            // 注册SignalR
            builder.Services.AddSignalR();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            //先跨域，再认证，最后授权
            app.UseCors("Any");
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<ExceptionMiddleware>();

            app.MapControllers();
            
            // 映射SignalR Hub
            app.MapHub<NotificationHub>("/notificationHub");

            app.Run();
        }
    }
}
