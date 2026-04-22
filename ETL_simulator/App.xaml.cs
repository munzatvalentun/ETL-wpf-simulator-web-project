using System.Windows;
using ETL_simulator.ETL;
using ETL_simulator.Generator;
using ETL_simulator.ViewModels;
using ETL_simulator.Views;
using ETL_web_project.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ETL_simulator
{
    public partial class App : Application
    {
        private IServiceProvider _services = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var services = new ServiceCollection();

            services.AddDbContext<ProjectContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")),
                ServiceLifetime.Transient);

            services.AddSingleton<GeneratorConfig>();
            services.AddTransient<SalesGenerator>();
            services.AddTransient<BronzeLoader>();
            services.AddTransient<SilverProcessor>();
            services.AddTransient<GoldLoader>();
            services.AddSingleton<MainViewModel>(sp =>
                new MainViewModel(sp, sp.GetRequiredService<GeneratorConfig>()));
            services.AddTransient<MainWindow>();

            _services = services.BuildServiceProvider();

            var window = _services.GetRequiredService<MainWindow>();
            window.DataContext = _services.GetRequiredService<MainViewModel>();
            window.Show();
        }
    }
}
