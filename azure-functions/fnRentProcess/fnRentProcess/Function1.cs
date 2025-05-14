using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Esta Azure Function é acionada automaticamente sempre que uma nova mensagem é recebida na fila "fila-locacoes" do Service Bus.
// Ela processa os dados da locação, salva as informações no banco de dados SQL e, em seguida, envia os mesmos dados para a fila "payment-queue".
// Essa arquitetura é usada para desacoplar o processamento da locação e o processo de pagamento.

namespace fnRentProcess
{
    public class ProcessaLocacao
    {
        private readonly ILogger<ProcessaLocacao> _logger;
        private readonly IConfiguration _config;

        public ProcessaLocacao(ILogger<ProcessaLocacao> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [Function(nameof(ProcessaLocacao))]
        public async Task Run(
            [ServiceBusTrigger("fila-locacoes", Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            // Log básico da mensagem recebida
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            var body = message.Body.ToString();
            _logger.LogInformation("Message Body: {body}", body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            RentModel.Rootobject rentModel = null;

            try
            {
                // Deserializa o JSON recebido para o modelo
                rentModel = JsonSerializer.Deserialize<RentModel.Rootobject>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (rentModel is null)
                {
                    _logger.LogError("Mensagem mal formatada");
                    await messageActions.DeadLetterMessageAsync(message, null, "Mensagem mal formatada");
                    return;
                }

                // Conexão com o banco e inserção dos dados da locação
                var connectionString = _config.GetConnectionString("SqlConnectionString");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    INSERT INTO Locacao (Nome, Email, Modelo, Ano, TempoAluguel, Data)
                    VALUES (@Nome, @Email, @Modelo, @Ano, @TempoAluguel, @Data)", connection);

                command.Parameters.AddWithValue("@Nome", rentModel.nome);
                command.Parameters.AddWithValue("@Email", rentModel.email);
                command.Parameters.AddWithValue("@Modelo", rentModel.veiculo.modelo);
                command.Parameters.AddWithValue("@Ano", rentModel.veiculo.ano);
                command.Parameters.AddWithValue("@TempoAluguel", rentModel.veiculo.tempoAluguel);
                command.Parameters.AddWithValue("@Data", rentModel.data);

                // Executa o comando SQL (inserção)
                var rowsAffected = await command.ExecuteNonQueryAsync();
                connection.Close();

                // Envia uma nova mensagem para a fila de pagamento
                var serviceBusConnection = _config.GetValue<string>("ServiceBusConnection");
                var serviceBusPayQueue = _config.GetValue<string>("ServiceBusPayQueue");
                await sendMessageToPay(serviceBusConnection, serviceBusPayQueue, rentModel);

                // Marca a mensagem original como processada com sucesso
                await messageActions.CompleteMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar a mensagem: {messageId}", message.MessageId);
                await messageActions.DeadLetterMessageAsync(message, null, $"Erro ao processar a mensagem: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Envia uma nova mensagem para a fila de pagamento, contendo as informações da locação.
        /// </summary>
        private async Task sendMessageToPay(string serviceBusConnection, string serviceBusPayQueue, RentModel.Rootobject rentModel)
        {
            ServiceBusClient serviceBusClient = new ServiceBusClient(serviceBusConnection);
            ServiceBusSender serviceBusSender = serviceBusClient.CreateSender(serviceBusPayQueue);

            var message = new ServiceBusMessage(JsonSerializer.Serialize(rentModel));
            message.ContentType = "application/json";

            // Adiciona propriedades customizadas para facilitar filtros/entendimento no destino
            message.ApplicationProperties.Add("Tipo", "Pagamento");
            message.ApplicationProperties.Add("Nome", rentModel.nome);
            message.ApplicationProperties.Add("Email", rentModel.email);
            message.ApplicationProperties.Add("Modelo", rentModel.veiculo.modelo);
            message.ApplicationProperties.Add("Ano", rentModel.veiculo.ano);
            message.ApplicationProperties.Add("TempoAluguel", rentModel.veiculo.tempoAluguel);
            message.ApplicationProperties.Add("Data", rentModel.data.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK"));

            await serviceBusSender.SendMessageAsync(message);
            await serviceBusSender.DisposeAsync();
        }
    }
}
