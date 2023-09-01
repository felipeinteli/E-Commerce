using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


namespace Fiap.Cartao
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "hello",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null
                                 );

            Console.WriteLine(" [*] Aguardando novas mensagens.");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($" [x] recebido {message}");

                var cartao = await ValidarCartao2();

                if(cartao is null)
                {
                    Console.WriteLine("Erro ao validar cartão");
                    //tag, multiplos, reenfileirar
                    channel.BasicNack(ea.DeliveryTag, false, false);
                }

                Console.WriteLine("Cartão validado com sucesso");
                channel.BasicAck(ea.DeliveryTag, false);

            };
            channel.BasicConsume(queue: "hello",
                                 autoAck: false,
                                 consumer: consumer);

            Console.WriteLine(" Pressione [enter] para finalizar.");
            Console.ReadLine();
        }

        static async Task<Cartao> ValidarCartao()
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetFromJsonAsync<Cartao>("http://demo7023384.mockable.io/validar-cartao");

            if (response == null)
            {
                Console.WriteLine("Erro ao validar cartão");
                return null;
            }

            return response;
        }

        static async Task<Cartao> ValidarCartao2()
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("http://demo7023384.mockable.io/validar-cartao");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Erro ao validar cartão");
                throw new Exception($"Erro ao validar. Status code: {response.StatusCode}");
            }

            return JsonSerializer.Deserialize<Cartao>(response.Content.ReadAsStream());
        }
    }

    public class Cartao
    {
        public string idPedido { get; set; }
        public string numeroCartao { get; set; }
        public string portador { get; set; }
        public int cvv { get; set; }
        public string vencimento { get; set; }
    }

}