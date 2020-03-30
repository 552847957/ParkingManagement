using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Park.Admin.Models;
using FineUICore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Park.Admin
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            services.AddDistributedMemoryCache();
            services.AddSession();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.LoginPath = new PathString("/Login");
                options.Cookie.HttpOnly = true;
            });

            // ���������������
            services.Configure<FormOptions>(x =>
            {
                x.ValueCountLimit = 1024;   // ��������ĸ������ƣ�Ĭ��ֵ��1024��
                x.ValueLengthLimit = 4194304;   // �����������ֵ�ĳ������ƣ�Ĭ��ֵ��4194304 = 1024 * 1024 * 4��
            });

            // FineUI ����
            services.AddFineUI(Configuration);

            services.AddRazorPages().AddMvcOptions(options =>
            {
                // �Զ���ģ�Ͱ󶨣�Newtonsoft.Json��
                options.ModelBinderProviders.Insert(0, new JsonModelBinderProvider());
            }).AddNewtonsoftJson();


            // �������ݿ������ַ�����Ŀǰ���� SQL Server �²���ͨ����
            services.AddDbContext<ParkAdminContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("SQLServer")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseSession();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // FineUI �м����ȷ�� UseFineUI λ�� UseEndpoints ��ǰ�棩
            app.UseFineUI();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}