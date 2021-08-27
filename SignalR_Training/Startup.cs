using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SignalR_Training.Hubs;
using DAL.EF;
using Microsoft.EntityFrameworkCore;
using BLL.Helpers;
using DAL.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace SignalR_Training
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string connection = Configuration.GetConnectionString("DefaultConnection");
            //services.AddMvcCore().AddApiExplorer().AddAuthorization();
            services.AddDbContext<AppDBContext>(options => options.UseSqlServer(connection), ServiceLifetime.Transient);
            services.AddRazorPages();
            #region SignalR
            //https://docs.microsoft.com/ru-ru/aspnet/core/tutorials/signalr?view=aspnetcore-3.1&tabs=visual-studio
            //https://metanit.com/sharp/aspnet5/30.3.php
            services.AddSignalR(hubOptions =>
            {
                hubOptions.KeepAliveInterval = System.TimeSpan.FromMinutes(4);
            });
            #endregion
            var appSettingsSection = Configuration.GetSection("AppSettings");
            var appSettings = appSettingsSection.Get<AppSettings>();
            services.AddSingleton<AppSettings>(appSettings);

            #region JWT_Setting
            //--------- JWT settingd ---------------------

            services.AddIdentity<User, IdentityRole>()
               .AddEntityFrameworkStores<AppDBContext>()
               .AddDefaultTokenProviders(); ;

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings.
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 0;

                // Lockout settings.
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings.
                options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;

                //options.SignIn.RequireConfirmedEmail = true;
            });            
            var key = System.Text.Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
          .AddJwtBearer(options =>
          {
              options.RequireHttpsMetadata = true;//if false - do not use SSl
              options.SaveToken = true;
              options.Events = new JwtBearerEvents()
              {
                  OnMessageReceived = context =>
                  {
                      var accessToken = context.Request.Query["access_token"];

                      // если запрос направлен хабу
                      var path = context.HttpContext.Request.Path;
                      if (!string.IsNullOrEmpty(accessToken) &&
                          (path.StartsWithSegments("/chatHub")))
                      {
                          // получаем токен из строки запроса
                          context.Token = accessToken;
                      }
                      return Task.CompletedTask;
                  },
                  OnAuthenticationFailed = (ctx) =>
                  {
                      if (ctx.Request.Path.StartsWithSegments("/chatHub") && ctx.Response.StatusCode == 200)
                      {
                          ctx.Response.StatusCode = 401;
                      }

                      return Task.CompletedTask;
                  },
                  OnForbidden = (ctx) =>
                  {
                      if (ctx.Request.Path.StartsWithSegments("/chatHub") && ctx.Response.StatusCode == 200)
                      {
                          ctx.Response.StatusCode = 403;
                      }

                      return Task.CompletedTask;
                  }
              };
              options.TokenValidationParameters = new TokenValidationParameters
              {
                  // specifies whether the publisher will be validated when validating the token 
                  ValidateIssuer = true,
                  // a string representing the publisher
                  ValidIssuer = appSettings.Issuer,

                  // whether the consumer of the token will be validated 
                  ValidateAudience = true,
                  // token consumer setting 
                  ValidAudience = appSettings.Audience,
                  // whether the lifetime will be validated 
                  ValidateLifetime = true,

                  // security key installation 
                  //IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
                  IssuerSigningKey = new SymmetricSecurityKey(key),
                  // security key validation 
                  ValidateIssuerSigningKey = true,
              };
          });
            //--------- JWT settingd ---------------------
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            AppDBContext applicationContext, UserManager<User> userManager)
        {
            //applicationContext.Database.Migrate();
            DbInitializer.Initialize(applicationContext, userManager);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapHub<ChatHub>("/chatHub",
                    options =>
                    {
                        options.ApplicationMaxBufferSize = 64;
                        options.TransportMaxBufferSize = 64;
                    });
            });
        }
    }
}
