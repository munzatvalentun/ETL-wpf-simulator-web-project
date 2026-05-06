using System.IO;
using System.IO.Pipes;

namespace ETL_simulator
{
    public class PipeListener
    {
        public const string PipeName = "etl-wpf-simulator";

        private readonly Action _onTrigger;

        public PipeListener(Action onTrigger)
        {
            _onTrigger = onTrigger;
        }

        public void Start(CancellationToken ct)
        {
            Task.Run(() => ListenAsync(ct), ct);
        }

        private async Task ListenAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        PipeName, PipeDirection.In, 1,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(ct);

                    using var reader = new StreamReader(server);
                    var cmd = await reader.ReadLineAsync();

                    if (cmd == "RUN")
                        App.Current.Dispatcher.Invoke(_onTrigger);
                }
                catch (OperationCanceledException) { break; }
                catch { /* ігноруємо помилки пайпу, продовжуємо слухати */ }
            }
        }
    }
}
