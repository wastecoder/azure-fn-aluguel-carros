# Sistema de Aluguel de Carros com Azure Functions e Service Bus

Este repositório contém o código-fonte de um desafio de projeto do bootcamp **Microsoft Azure Cloud Native** da DIO.
O objetivo deste sistema é simular um fluxo completo de aluguel de veículos utilizando tecnologias nativas da nuvem, como **Azure Functions**, **Azure Service Bus**, **Azure SQL Database** e **Cosmos DB**.
O sistema é composto por **uma API** para solicitação de locação e **duas Azure Functions** que processam a locação e o pagamento.


## :file_folder: Estrutura do Projeto

```bash
azure-fn-aluguel-carros/
├── rent-a-car/
└── azure-functions/
    ├── fnRentProcess/
    └── fnPayment/
```

* `rent-a-car/`: Contém a API desenvolvida com Node.js e Express.
  * Expõe o endpoint `POST /api/locacao` para que clientes possam solicitar a locação de veículos.
  * Valida os dados recebidos e envia uma mensagem para a fila `fila-locacoes` do Azure Service Bus.
  * Possui um [README](https://github.com/wastecoder/azure-fn-aluguel-carros/blob/main/rent-a-car/README.md) com detalhes técnicos da API e orientações para deploy no Azure Container Apps.  
* `fnRentProcess/`: Contém a Azure Function responsável por processar locações.
  * É acionada automaticamente por mensagens da fila `fila-locacoes`.
  * Insere os dados no banco de dados SQL.
  * Encaminha os dados da locação para a fila `payment-queue`.
* `fnPayment/`: Contém a Azure Function responsável por processar pagamentos.
  * É acionada por mensagens da fila `payment-queue`.
  * Define um status aleatório para o pagamento.
  * Armazena os dados no Azure Cosmos DB.
  * Se o pagamento for aprovado, envia uma notificação para a fila `notification-queue`.


## :cloud: Serviços do Microsoft Azure Utilizados

* **Grupo de Recursos:** Organiza todos os recursos relacionados ao projeto.
* **Azure Container Registry:** Armazena a imagem da API `rent-a-car`.
* **Azure Container Apps:** Hospeda e executa a API `rent-a-car`.
* **Azure SQL Database:** Armazena os dados das locações.
* **Azure SQL Server:** Gerencia o banco de dados SQL do projeto.
* **Azure Functions:** Executa as funções `fnRentProcess` e `fnPayment`.
* **Azure Service Bus:** Faz a comunicação entre API, funções e Logic App.
* **Application Insights:** Coleta logs e métricas das Azure Functions.
* **Azure Cosmos DB:** Armazena os dados dos pagamentos processados.
* **Azure Logic Apps:** Escuta a `notification-queue` e envia notificações.
* **Azure Storage Account:** Suporte para logs e controle das Azure Functions.
* **Azure Key Vault:** Armazena secrets e strings de conexão com segurança.


## :gear: Processo de Criação

1. **Criação dos Recursos no Azure:**
   * Todos os recursos foram criados e organizados em um único Grupo de Recursos.
   * Inclui Service Bus, SQL Server, Cosmos DB, Logic App, Storage Account, entre outros.

2. **Desenvolvimento da API (`rent-a-car/`):**
   * Desenvolvida com Node.js e Express.
   * Publicada no Azure Container Apps.
   * Envia os dados recebidos para a fila `fila-locacoes` do Azure Service Bus.
   * **Resumo:** Recebe a locação e envia para a fila de locação.

3. **Azure Function de Locação (`fnRentProcess`):**
   * **Gatilho:** Fila `fila-locacoes` do Azure Service Bus.
   * Insere os dados da locação no banco de dados SQL.
   * Envia os mesmos dados para a fila `payment-queue`.
   * **Resumo:** Recebe a locação que veio da fila de locação e envia para a fila de pagamento.

4. **Azure Function de Pagamento (`fnPayment`):**
   * **Gatilho:** Fila `payment-queue` do Azure Service Bus.
   * Define um status aleatório: `Aprovado`, `Reprovado` ou `Em análise`.
   * Salva os dados no Cosmos DB.
   * Se o status for `Aprovado`, envia mensagem para a fila `notification-queue`.
   * **Resumo:** Salva no Cosmos DB o que veio da fila de pagamento e avança o fluxo se aprovado.

5. **Logic App de Notificação:**
   * **Gatilho:** Fila `notification-queue` do Azure Service Bus.
   * Verifica a fila a cada 3 minutos.
   * Quando há uma nova mensagem, um e-mail é enviado automaticamente ao cliente.
   * O e-mail informa a aprovação do pagamento e a confirmação da locação.
   * **Resumo:** É a última etapa do fluxo, notificando o cliente caso seu pagamento seja aprovado.


## :bulb: Insights e Possibilidades
* **Desacoplamento entre componentes com Service Bus:** O uso do Azure Service Bus garante que cada etapa (locação, pagamento e notificação) funcione de forma independente e tolerante a falhas, facilitando a manutenção e escalabilidade.
* **Execução sob demanda com Azure Functions:** O modelo serverless permite que cada função só seja executada quando necessário, reduzindo custos e evitando o gerenciamento de servidores.
* **Processamento assíncrono e resiliente:** A comunicação por filas garante que mesmo em picos de uso ou falhas temporárias, as mensagens de locação e pagamento não sejam perdidas.
* **Escalabilidade automática:** As Azure Functions escalam automaticamente com base na demanda, o que é ideal para sistemas com volume variável de requisições.
* **Separação clara de responsabilidades:** Cada Azure Function é responsável por uma etapa distinta do processo, facilitando testes, manutenção e entendimento do fluxo.
* **Pronto para novas integrações:** O sistema pode ser facilmente expandido com novas filas e funções, como envio de SMS, integração com gateways de pagamento reais ou relatórios de uso.


## :computer: Instalação e Execução

Para executar este projeto e testar todo o fluxo de aluguel de carros:

1. **Crie os recursos no Azure:**
   * No Portal Azure, crie um Grupo de Recursos e adicione nele os serviços necessários: Service Bus, SQL Server, Banco de Dados SQL, Cosmos DB, Container Apps, Logic App, etc.
   * Verifique que os nomes das filas (`fila-locacoes`, `payment-queue`, `notification-queue`) e demais recursos estejam alinhados com o código e variáveis de ambiente.

2. **Configure a API:**
   * Basta criar um arquivo `.env` com a variável indicada no [README da API](https://github.com/wastecoder/azure-fn-aluguel-carros/blob/main/rent-a-car/README.md).
   * Essas variáveis permitem que a API envie corretamente os dados para a fila `fila-locacoes` do Service Bus.
   * O mesmo README também mostra como fazer o deploy da API diretamente no Portal Azure usando a Azure CLI.

3. **Configure as variáveis de ambiente das Azure Functions:**
   * No ambiente local, use o arquivo `local.settings.json`.
   * Após o deploy, configure as mesmas variáveis manualmente no Portal Azure.

4. **Variáveis de ambiente da função de locação**
   * Exemplo de `local.settings.json` localmente:
```json
{
  "IsEncrypted": false,
  "Values": {
    // ... outras configurações ...
    "ServiceBusConnection": "Endpoint=sb://[...]",
    "ServiceBusPayQueue": "payment-queue"
  },
  "ConnectionStrings": {
    "SqlConnectionString": "Server=tcp:[...];Database=...;User Id=...;Password=...;"
  }
}
```
   * No Portal Azure, após publicar esta Function, configure as seguintes variáveis de ambiente:
     * `ServiceBusConnection`
     * `ServiceBusPayQueue`
     * `SqlConnectionString` (em Connection Strings, não em Application Settings)

5. **Variáveis de ambiente da função de pagamento**
   * Exemplo de `local.settings.json` localmente:
```json
{
  "IsEncrypted": false,
  "Values": {
    // ... outras configurações ...
    "ServiceBusConnection": "Endpoint=sb://[...]",
    "CosmosDb": "paymentDB-01",
    "CosmosContainer": "paymentcontainer01",
    "CosmosDBConnection": "AccountEndpoint=https://[...]",
    "NotificationQueue": "notification-queue"
  }
}
```
   * No Portal Azure, após publicar esta Function, configure as seguintes variáveis de ambiente:
     * `ServiceBusConnection`
     * `CosmosDb`
     * `CosmosContainer`
     * `NotificationQueue`

6. **Configure o firewall do SQL Server:**
   * Sem essa configuração, não será possível se conectar ao banco de dados.
   * Executando **localmente**: adicione seu IP ao firewall no Portal Azure.
   * Executando **na nuvem**: ative a opção "Permitir serviços do Azure".
   * Para isso, acesse o recurso **SQL Server** e vá em **Definir o firewall do servidor**.

7. **Execute localmente ou publique na nuvem:**
   * A API (pasta `rent-a-car/`) e as Azure Functions podem ser executadas localmente ou publicadas no Azure.
   * No caso da API, ao publicar no Container Apps, copie o **FQDN** gerado e use-o como base para as requisições.
   * Para as Functions, após o deploy, configure todas as variáveis de ambiente no Portal Azure.

8. **Teste com uma requisição:**
   * Teste o fluxo usando o Postman ou outro cliente HTTP após a API e as Functions estarem em execução.
   * Envie uma requisição `POST` para o endpoint `/api/locacao` da API com o seguinte corpo:
     ```json
     {
       "nome": "João Silva",
       "email": "joao@email.com",
       "modelo": "HB20",
       "ano": 2020,
       "tempoAluguel": "3 dias"
     }
     ```
   * Isso iniciará o fluxo completo.

9. **Valide os resultados:**
   * Após o teste, verifique se os dados foram processados corretamente:
   * Mensagens consumidas das filas `fila-locacoes`, `payment-queue` e `notification-queue`.
   * Dados da locação inseridos no Banco de Dados SQL.
   * Dados do pagamento (com status) salvos no Cosmos DB.
   * E-mail enviado ao cliente, se o pagamento tiver sido aprovado.

> Observação: o tempo de propagação entre uma etapa e outra pode levar alguns segundos devido ao uso do Azure Service Bus e execução assíncrona entre as funções.
