using Microsoft.EntityFrameworkCore;
using System;
using TravelSystem.Hubs;
using TravelSystem.Models;
using TravelSystem.Services;
using TravelSystemService.Services;

namespace TravelSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<FinalPrnContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));
            builder.Services.AddSignalR();
            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddSession();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auths/Login";
                options.AccessDeniedPath = "/Auths/Login"; // Nếu bị từ chối quyền cũng quay về đây
            });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<PayOSService>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();
            
            app.MapRazorPages();
            app.MapHub<HubServer>("/hub");
            app.Run();
        }
    }
}
