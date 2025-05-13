# API de Locação de Carros com Azure Service Bus

Este repositório contém parte de uma aplicação desenvolvida durante um laboratório do bootcamp **Microsoft Azure Cloud Native** da DIO.
A aplicação representa o back-end responsável por receber os dados de uma locação de carro e enviá-los para uma fila no **Azure Service Bus**.
Essa arquitetura permite desacoplar os sistemas e escalar a comunicação de forma segura e eficiente.


## :file_folder: Estrutura da Pasta

```bash
rent-a-car/
├── index.js
├── .env
├── Dockerfile
└── publicarContainerNoAzure.ps1
```

* `index.js`: Código principal da API Express responsável por receber requisições e enviá-las à fila.
* `.env`: Arquivo com a variável de conexão do Azure Service Bus.
* `Dockerfile`: Responsável por criar a imagem do contêiner da aplicação.
* `publicarContainerNoAzure.ps1`: Script com os comandos necessários para fazer o deploy do contêiner no Azure Container Apps.


## ⚙️ Instalação e Configuração

Siga os passos abaixo para rodar o back-end localmente ou publicá-lo no Azure:

1. **Instalar dependências**
   ```bash
   npm install
   ```
   Isso instalará os pacotes definidos no `package.json`.

2. **Criar o arquivo `.env`**

   Na raiz do projeto (`rent-a-car/`), crie um arquivo `.env` com a seguinte variável:
   ```
   AZURE_SERVICE_BUS_CONNECTION_STRING=Endpoint=sb://...
   ```
   Você encontra essa conexão no portal do Azure, dentro do seu recurso de Service Bus, em "Políticas de acesso compartilhado".

3. **Criar a imagem Docker**

   Execute o comando abaixo na raiz do projeto:
   ```bash
   docker build -t bff-rent-car-local .
   ```
   ⚠️ **Certifique-se de que o Docker Desktop esteja rodando** antes de executar o comando.

4. **Rodar o contêiner localmente**
   ```bash
   docker run -d -p 3001:3001 bff-rent-car-local
   ```
   Isso expõe a API localmente na porta 3001.

5. **Deploy no Azure**

   No arquivo `publicarContainerNoAzure.ps1` há uma lista de comandos para fazer o deploy do contêiner `bff-rent-car-local` usando o Azure Container Apps.

   ⚠️ Atenção: É necessário ativar a opção de **"Usuário administrador"** no Azure Container Registry e inserir o **nome de usuário** e **senha** nos comandos onde indicado.


## 🧪 Testando a Aplicação

### Envio de requisições

Você pode testar a aplicação com uma ferramenta como **Postman** ou **cURL**, enviando uma requisição `POST` para:

- Localmente: `http://localhost:3001/api/locacao`
- No Azure: `https://<SEU_FQDN_AQUI>/api/locacao`

> Exemplo real:
> `https://bff-rent-car-local--w1h1x60.politefield-16036730.eastus.azurecontainerapps.io/api/locacao`

### Corpo da Requisição (Body)

```json
{
  "nome": "João Silva",
  "email": "joao@email.com",
  "modelo": "HB20",
  "ano": 2020,
  "tempoAluguel": "3 dias"
}
```

Se estiver tudo correto, você receberá a resposta:
```
Locação enviada para a fila com sucesso.
```

### Visualizando a mensagem no Azure

Após enviar a requisição, você pode verificar se a mensagem chegou à fila do Service Bus:

1. Acesse o recurso **Service Bus** no Portal Azure.
2. No menu lateral, clique em **Filas** e selecione a fila configurada (ex: `fila-locacoes`).
3. Em seguida, clique em **Gerenciador de Barramento de Serviço** no menu da fila selecionada.
4. Clique no botão **Espiar desde o início** para visualizar as mensagens recebidas.

Assim você poderá confirmar que a mensagem foi enfileirada com sucesso.
