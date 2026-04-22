using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ETL_simulator.ETL;
using ETL_simulator.Generator;
using Microsoft.Extensions.DependencyInjection;

namespace ETL_simulator.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider _services;
        private readonly GeneratorConfig _config;

        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private int _batchSize = 10;
        private double _errorRate = 15;
        private bool _enableDuplicates = true;
        private int _delayMs = 1500;

        // Stats
        private int _totalGenerated;
        private int _passedSilver;
        private int _rejected;
        private int _loadedToGold;

        // Stage counts
        private int _bronzeTotal;
        private int _silverTotal;
        private int _goldTotal;
        private string _bronzeStatus = "Idle";
        private string _silverStatus = "Idle";
        private string _goldStatus  = "Idle";

        public ObservableCollection<LogEntry> Logs { get; } = new();

        public ICommand StartCommand { get; }
        public ICommand StopCommand  { get; }
        public ICommand StepCommand  { get; }
        public ICommand ClearLogCommand { get; }

        public MainViewModel(IServiceProvider services, GeneratorConfig config)
        {
            _services = services;
            _config   = config;

            StartCommand    = new RelayCommand(Start,    () => !IsRunning);
            StopCommand     = new RelayCommand(Stop,     () => IsRunning);
            StepCommand     = new RelayCommand(StepOnce, () => !IsRunning);
            ClearLogCommand = new RelayCommand(() => Logs.Clear());
        }

        #region Properties

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                OnPropertyChanged();
                App.Current.Dispatcher.Invoke(CommandManager.InvalidateRequerySuggested);
            }
        }

        public int BatchSize
        {
            get => _batchSize;
            set { _batchSize = value; _config.BatchSize = value; OnPropertyChanged(); }
        }

        public double ErrorRate
        {
            get => _errorRate;
            set
            {
                _errorRate = value;
                double r = value / 100.0;
                _config.NullStoreRate       = r * 0.4;
                _config.NullProductRate     = r * 0.4;
                _config.NegativeQuantityRate = r * 0.2;
                OnPropertyChanged();
            }
        }

        public bool EnableDuplicates
        {
            get => _enableDuplicates;
            set
            {
                _enableDuplicates = value;
                _config.DuplicateRate = value ? 0.05 : 0.0;
                OnPropertyChanged();
            }
        }

        public int DelayMs
        {
            get => _delayMs;
            set { _delayMs = value; OnPropertyChanged(); }
        }

        public int TotalGenerated { get => _totalGenerated; private set { _totalGenerated = value; OnPropertyChanged(); } }
        public int PassedSilver   { get => _passedSilver;   private set { _passedSilver   = value; OnPropertyChanged(); } }
        public int Rejected       { get => _rejected;       private set { _rejected       = value; OnPropertyChanged(); } }
        public int LoadedToGold   { get => _loadedToGold;   private set { _loadedToGold   = value; OnPropertyChanged(); } }

        public int BronzeTotal    { get => _bronzeTotal;  private set { _bronzeTotal  = value; OnPropertyChanged(); } }
        public int SilverTotal    { get => _silverTotal;  private set { _silverTotal  = value; OnPropertyChanged(); } }
        public int GoldTotal      { get => _goldTotal;    private set { _goldTotal    = value; OnPropertyChanged(); } }
        public string BronzeStatus { get => _bronzeStatus; private set { _bronzeStatus = value; OnPropertyChanged(); } }
        public string SilverStatus { get => _silverStatus; private set { _silverStatus = value; OnPropertyChanged(); } }
        public string GoldStatus   { get => _goldStatus;   private set { _goldStatus   = value; OnPropertyChanged(); } }

        #endregion

        private void Start()
        {
            _cts = new CancellationTokenSource();
            IsRunning = true;
            AddLog(LogLevel.Info, "Симуляцію запущено");
            Task.Run(() => RunLoopAsync(_cts.Token));
        }

        private void Stop()
        {
            _cts?.Cancel();
            IsRunning = false;
            BronzeStatus = SilverStatus = GoldStatus = "Idle";
            AddLog(LogLevel.Warning, "Симуляцію зупинено");
        }

        private async void StepOnce()
        {
            AddLog(LogLevel.Info, "Ручний крок...");
            await RunStepAsync();
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            // Seed dimensions once at start
            using var scope = _services.CreateScope();
            var gold = scope.ServiceProvider.GetRequiredService<GoldLoader>();
            await gold.SeedDimensionsAsync();
            AddLog(LogLevel.Info, "Виміри (DimStore, DimProduct) перевірено/заповнено");

            while (!ct.IsCancellationRequested)
            {
                await RunStepAsync();
                try { await Task.Delay(DelayMs, ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task RunStepAsync()
        {
            using var scope = _services.CreateScope();
            var generator = scope.ServiceProvider.GetRequiredService<SalesGenerator>();
            var bronze    = scope.ServiceProvider.GetRequiredService<BronzeLoader>();
            var silver    = scope.ServiceProvider.GetRequiredService<SilverProcessor>();
            var gold      = scope.ServiceProvider.GetRequiredService<GoldLoader>();

            // Bronze
            try
            {
                BronzeStatus = "Loading...";
                var batch = generator.GenerateBatch(BatchSize);
                var br = await bronze.LoadAsync(batch);
                BronzeTotal += br.Inserted;
                TotalGenerated += br.Inserted;
                BronzeStatus = $"+{br.Inserted}";
                AddLog(LogLevel.Info, $"[Bronze] Завантажено {br.Inserted} сирих записів");
            }
            catch (Exception ex)
            {
                BronzeStatus = "Error";
                AddLog(LogLevel.Error, $"[Bronze] {GetDeepMessage(ex)}");
                return;
            }

            // Silver
            try
            {
                SilverStatus = "Processing...";
                var sr = await silver.ProcessAsync();
                SilverTotal += sr.Inserted;
                PassedSilver += sr.Inserted;
                Rejected += sr.Rejected;
                SilverStatus = $"+{sr.Inserted}";
                if (sr.Rejected > 0)
                {
                    AddLog(LogLevel.Warning, $"[Silver] Прийнято: {sr.Inserted} | Відхилено: {sr.Rejected}");
                    if (sr.NullStore    > 0) AddLog(LogLevel.Warning, $"  ↳ Null StoreCode: {sr.NullStore}");
                    if (sr.NullProduct  > 0) AddLog(LogLevel.Warning, $"  ↳ Null ProductCode: {sr.NullProduct}");
                    if (sr.BadQuantity  > 0) AddLog(LogLevel.Warning, $"  ↳ Некоректна кількість (≤0): {sr.BadQuantity}");
                    if (sr.BadPrice     > 0) AddLog(LogLevel.Warning, $"  ↳ Некоректна ціна (≤0): {sr.BadPrice}");
                    if (sr.NullDate     > 0) AddLog(LogLevel.Warning, $"  ↳ Відсутня дата: {sr.NullDate}");
                    if (sr.Duplicates   > 0) AddLog(LogLevel.Warning, $"  ↳ Дублікати: {sr.Duplicates}");
                }
                else
                    AddLog(LogLevel.Info, $"[Silver] Прийнято: {sr.Inserted}");
            }
            catch (Exception ex)
            {
                SilverStatus = "Error";
                AddLog(LogLevel.Error, $"[Silver] {GetDeepMessage(ex)}");
                return;
            }

            // Gold
            try
            {
                GoldStatus = "Loading...";
                var gr = await gold.LoadAsync();
                GoldTotal += gr.Inserted;
                LoadedToGold += gr.Inserted;
                GoldStatus = $"+{gr.Inserted}";
                AddLog(LogLevel.Info, $"[Gold] Завантажено до FactSales: {gr.Inserted}");
            }
            catch (Exception ex)
            {
                GoldStatus = "Error";
                AddLog(LogLevel.Error, $"[Gold] {GetDeepMessage(ex)}");
            }
        }

        private static string GetDeepMessage(Exception ex)
        {
            var inner = ex;
            while (inner.InnerException != null)
                inner = inner.InnerException;
            return inner.Message;
        }

        private void AddLog(LogLevel level, string message)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, new LogEntry { Level = level, Message = message });
                if (Logs.Count > 200) Logs.RemoveAt(Logs.Count - 1);
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
