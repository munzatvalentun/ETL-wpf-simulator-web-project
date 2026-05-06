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
        private CancellationTokenSource _pipeCts = new();

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
            services.AddTransient<EtlOrchestrator>();
            services.AddSingleton<MainViewModel>(sp =>
                new MainViewModel(sp, sp.GetRequiredService<GeneratorConfig>()));
            services.AddTransient<MainWindow>();

            _services = services.BuildServiceProvider();

            var mainVm = _services.GetRequiredService<MainViewModel>();
            new PipeListener(mainVm.TriggerExternalRun).Start(_pipeCts.Token);

            var window = _services.GetRequiredService<MainWindow>();
            window.DataContext = mainVm;
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _pipeCts.Cancel();
            base.OnExit(e);
        }
    }
}
