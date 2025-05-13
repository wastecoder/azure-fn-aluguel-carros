const express = require("express");
const cors = require("cors");
const { ServiceBusClient } = require("@azure/service-bus");
require("dotenv").config();

const app = express();
app.use(cors());
app.use(express.json());

// Função para montar a mensagem
function criarMensagem({ nome, email, modelo, ano, tempoAluguel }) {
  return {
    nome,
    email,
    veiculo: {
      modelo,
      ano,
      tempoAluguel,
    },
    data: new Date().toISOString(),
  };
}

app.post("/api/locacao", async (req, res) => {
  const { nome, email, modelo, ano, tempoAluguel } = req.body;

  // Validação simples dos campos obrigatórios
  if (!nome || !email || !modelo || !ano || !tempoAluguel) {
    return res.status(400).send("Campos obrigatórios ausentes.");
  }

  const serviceBusConnection = process.env.AZURE_SERVICE_BUS_CONNECTION_STRING;
  const mensagem = criarMensagem({ nome, email, modelo, ano, tempoAluguel });

  try {
    const sbClient = new ServiceBusClient(serviceBusConnection);
    const sender = sbClient.createSender("fila-locacoes");

    const message = {
      body: mensagem,
      contentType: "application/json",
    };

    await sender.sendMessages(message);
    await sender.close();
    await sbClient.close();

    res.status(200).send("Locação enviada para a fila com sucesso.");
  } catch (err) {
    console.error("Erro ao enviar mensagem para a fila:", err.message);
    res.status(500).send("Erro interno no servidor.");
  }
});

app.listen(3001, () => console.log("BFF rodando na porta 3001"));
