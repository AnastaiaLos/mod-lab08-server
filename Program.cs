using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace ServerSimulation
{
    // Аргументы события запроса
    public class RequestEventArgs : EventArgs
    {
        public int ClientId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Класс клиента
    public class Client
    {
        public int Id { get; private set; }
        public event EventHandler<RequestEventArgs> RequestSent;

        public Client(int id)
        {
            Id = id;
        }

        public void SendRequest()
        {
            var args = new RequestEventArgs { ClientId = Id, Timestamp = DateTime.Now };
            RequestSent?.Invoke(this, args);
        }
    }

    // Класс канала обслуживания
    public class ServiceChannel
    {
        public int Id { get; private set; }
        public bool IsBusy { get; private set; }
        public event EventHandler<int> ChannelFreed;

        public ServiceChannel(int id)
        {
            Id = id;
            IsBusy = false;
        }

        public async void ProcessRequest(RequestEventArgs request, double serviceIntensity)
        {
            IsBusy = true;
            Random rand = new Random();
            double serviceTime = -Math.Log(1 - rand.NextDouble()) / serviceIntensity;
            await Task.Delay((int)(serviceTime * 1000));
            IsBusy = false;
            ChannelFreed?.Invoke(this, Id);
        }
    }

    // Класс сервера
    public class Server
    {
        private List<ServiceChannel> channels;
        private int totalRequests = 0;
        private int processedRequests = 0;
        private int rejectedRequests = 0;
        private double serviceIntensity;

        public Server(int channelCount, double serviceIntensity)
        {
            channels = new List<ServiceChannel>();
            for (int i = 0; i < channelCount; i++)
            {
                var channel = new ServiceChannel(i);
                channel.ChannelFreed += OnChannelFreed;
                channels.Add(channel);
            }
            this.serviceIntensity = serviceIntensity;
        }

        public void OnRequestReceived(object sender, RequestEventArgs e)
        {
            totalRequests++;
            var freeChannel = channels.FirstOrDefault(c => !c.IsBusy);
            if (freeChannel != null)
            {
                processedRequests++;
                freeChannel.ProcessRequest(e, serviceIntensity);
            }
            else
            {
                rejectedRequests++;
            }
        }

        private void OnChannelFreed(object sender, int channelId)
        {
            // Канал освободился
        }

        public (int total, int processed, int rejected) GetStatistics()
        {
            return (totalRequests, processedRequests, rejectedRequests);
        }

        public void Reset()
        {
            totalRequests = 0;
            processedRequests = 0;
            rejectedRequests = 0;
        }
    }

    // Результаты моделирования
    public class SimulationResult
    {
        public double Lambda { get; set; }
        public double TheoreticalP0 { get; set; }
        public double TheoreticalPRejection { get; set; }
        public double TheoreticalQ { get; set; }
        public double TheoreticalA { get; set; }
        public double TheoreticalK { get; set; }
        public double ExperimentalPRejection { get; set; }
        public double ExperimentalQ { get; set; }
        public double ExperimentalA { get; set; }
        public double ExperimentalK { get; set; }
        public double ExperimentalP0 { get; set; }
    }

    class Program
    {
        static int Factorial(int n)
        {
            int result = 1;
            for (int i = 2; i <= n; i++)
                result *= i;
            return result;
        }

        static (double p0, double pRejection, double q, double a, double k) CalculateTheoretical(int n, double lambda, double mu)
        {
            double rho = lambda / mu;
            double sum = 0;
            for (int i = 0; i <= n; i++)
            {
                sum += Math.Pow(rho, i) / Factorial(i);
            }
            double p0 = 1 / sum;
            double pRejection = (Math.Pow(rho, n) / Factorial(n)) * p0;
            double q = 1 - pRejection;
            double a = lambda * q;
            double k = rho * (1 - pRejection);
            return (p0, pRejection, q, a, k);
        }

        static SimulationResult RunExperiment(int channelCount, double lambda, double serviceIntensity, double simulationTimeMs)
        {
            var server = new Server(channelCount, serviceIntensity);
            var clients = new List<Client>();
            
            // Создаём 100 клиентов
            for (int i = 0; i < 100; i++)
            {
                var client = new Client(i);
                client.RequestSent += server.OnRequestReceived;
                clients.Add(client);
            }

            Random rand = new Random();
            int generatedRequests = 0;
            int totalProcessed = 0;
            int totalRejected = 0;
            
            double startTime = Environment.TickCount;
            double nextRequestTime = 0;
            
            while (Environment.TickCount - startTime < simulationTimeMs)
            {
                // Генерация запросов по экспоненциальному распределению
                if (nextRequestTime <= Environment.TickCount - startTime)
                {
                    generatedRequests++;
                    var client = clients[rand.Next(clients.Count)];
                    client.SendRequest();
                    
                    double interval = -Math.Log(1 - rand.NextDouble()) / lambda;
                    nextRequestTime += interval * 1000;
                }
                
                Thread.Sleep(1);
            }
            
            var stats = server.GetStatistics();
            double pRejection = stats.total > 0 ? (double)stats.rejected / stats.total : 0;
            double q = stats.total > 0 ? (double)stats.processed / stats.total : 0;
            double a = lambda * q;
            double k = (double)stats.processed / (simulationTimeMs / 1000) / serviceIntensity;
            
            // Теоретические расчёты
            var theoretical = CalculateTheoretical(channelCount, lambda, serviceIntensity);
            
            return new SimulationResult
            {
                Lambda = lambda,
                TheoreticalP0 = theoretical.p0,
                TheoreticalPRejection = theoretical.pRejection,
                TheoreticalQ = theoretical.q,
                TheoreticalA = theoretical.a,
                TheoreticalK = theoretical.k,
                ExperimentalPRejection = pRejection,
                ExperimentalQ = q,
                ExperimentalA = a,
                ExperimentalK = k,
                ExperimentalP0 = 1 - pRejection
            };
        }

        static void SaveResults(List<SimulationResult> results)
        {
            Directory.CreateDirectory("result");
            
            using (StreamWriter writer = new StreamWriter("result/analysis.txt"))
            {
                writer.WriteLine("λ\tP0_теор\tP_отк_теор\tQ_теор\tA_теор\tk_теор\tP_отк_эксп\tQ_эксп\tA_эксп\tk_эксп");
                foreach (var r in results)
                {
                    writer.WriteLine($"{r.Lambda:F2}\t{r.TheoreticalP0:F4}\t{r.TheoreticalPRejection:F4}\t{r.TheoreticalQ:F4}\t{r.TheoreticalA:F4}\t{r.TheoreticalK:F4}\t{r.ExperimentalPRejection:F4}\t{r.ExperimentalQ:F4}\t{r.ExperimentalA:F4}\t{r.ExperimentalK:F4}");
                }
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Моделирование СМО с отказами");

            int channelCount = 4;
            double serviceIntensity = 1.0;
            double simulationTimeMs = 60000;

            double[] lambdas = { 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0 };
            var results = new List<SimulationResult>();

            Console.WriteLine("\nИсследование зависимости параметров СМО от интенсивности λ\n");

            foreach (var lambda in lambdas)
            {
                Console.Write($"λ = {lambda:F1}: моделирование... ");
                var result = RunExperiment(channelCount, lambda, serviceIntensity, simulationTimeMs);
                results.Add(result);
                Console.WriteLine($"готово. P_отказа = {result.ExperimentalPRejection:F4} (теор: {result.TheoreticalPRejection:F4})");
            }

            SaveResults(results);
            Console.WriteLine("\nРезультаты сохранены в result/analysis.txt");
            Console.WriteLine("\nГотово!");
        }
    }
}
