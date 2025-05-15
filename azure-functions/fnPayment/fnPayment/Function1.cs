using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using fnPayment.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// Esta Azure Function é acionada automaticamente sempre que uma nova mensagem é recebida na fila "payment-queue" do Service Bus.
// Ela processa os dados de pagamento recebidos após uma locação, atribui um status aleatório (Aprovado, Reprovado ou Em análise)
// e, caso o pagamento seja aprovado, envia uma notificação para a fila "notification-queue".
// Além disso, os dados processados são armazenados em um banco Cosmos DB para histórico e rastreamento.
// Essa função é parte do fluxo de orquestração entre locação, pagamento e notificação.

namespace fnPayment
{
    public class Payment
    {
        private readonly ILogger<Payment> _logger;
        private readonly IConfiguration _configuration;
        private readonly string[] StatusList = { "Aprovado", "Reprovado", "Em análise" };
        private readonly Random random = new Random();

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public Payment(ILogger<Payment> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        // Função principal disparada pela fila do Service Bus (payment-queue)
        // Processa o pagamento e, se aprovado, envia notificação
        // Também armazena os dados processados no Cosmos DB para histórico.
        [Function(nameof(Payment))]
        [CosmosDBOutput("%CosmosDb%", "%CosmosContainer%", Connection = "CosmosDBConnection", CreateIfNotExists = true)]
        public async Task<object?> Run(
            [ServiceBusTrigger("payment-queue", Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            PaymentModel? payment = null;

            try
            {
                // Desserializa a mensagem recebida do Service Bus para um modelo de pagamento (PaymentModel).
                // Caso a mensagem seja inválida ou mal formatada, ela será movida para a Dead Letter Queue.
                payment = JsonSerializer.Deserialize<PaymentModel>(message.Body.ToString(), _jsonOptions);

                if (payment == null)
                {
                    await messageActions.DeadLetterMessageAsync(message, null, "Mensagem mal formatada");
                    return null;
                }

                // Sorteia aleatoriamente um dos três status possíveis: "Aprovado", "Reprovado" ou "Em análise"
                // e depois atribui o status ao pagamento.
                int index = random.Next(StatusList.Length);
                string status = StatusList[index];
                payment.status = status;

                // Gera um novo ID se ele estiver vazio
                if (payment.id == Guid.Empty)
                {
                    payment.id = Guid.NewGuid();
                }

                // Se o pagamento for aprovado, define a data de aprovação e envia o pagamento para a fila de notificação
                if (status == "Aprovado")
                {
                    payment.dataAprovacao = DateTime.UtcNow;
                    await SendToNotificationQueue(payment);
                }

                return payment;
            }
            catch (Exception ex)
            {
                // Em caso de erro ao processar a mensagem (por exemplo, falha de desserialização ou erro interno),
                // a mensagem é movida para a Dead Letter Queue para posterior investigação e diagnóstico.
                _logger.LogError(ex, "Erro ao processar o pagamento: {messageId}", message.MessageId);
                await messageActions.DeadLetterMessageAsync(message, null, $"Erro ao processar o pagamento: {ex.Message}");
                return null;
            }
            finally
            {
                await messageActions.CompleteMessageAsync(message);
            }
        }

        // Envia uma mensagem de notificação para a fila "notification-queue" do Service Bus.
        // A mensagem inclui os detalhes do pagamento aprovado e é usada para notificar sistemas ou usuários.
        private async Task SendToNotificationQueue(PaymentModel payment)
        {
            try
            {
                var connection = _configuration["ServiceBusConnection"];
                var queueName = _configuration["NotificationQueue"];

                await using var serviceBusClient = new ServiceBusClient(connection);
                ServiceBusSender sender = serviceBusClient.CreateSender(queueName);

                var message = new ServiceBusMessage(JsonSerializer.Serialize(payment));
                message.ContentType = "application/json";
                message.ApplicationProperties["type"] = "notification";
                message.ApplicationProperties["message"] = "Pagamento aprovado com sucesso";
                message.ApplicationProperties["timestamp"] = DateTime.UtcNow.ToString("o");

                await sender.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar notificação para a fila.");
            }
        }
    }
}
