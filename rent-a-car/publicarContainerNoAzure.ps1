# Requisitos:
# - Ter o Azure CLI instalado e logado na sua conta
# - Docker instalado e rodando
# - Já ter construído a imagem localmente com: docker build -t bff-rent-car-local .


# 1. Faça login na sua conta Azure
az login
# Depois de logar, selecione a conta (caso peça).


# 2. Faça login no Azure Container Registry (ACR)
az acr login --name acrlab07

# Se der erro, use:
# az acr login --name (container-registry-name) --resource-group (resource-group-name)


# 3. Verifique se a imagem Docker está listada localmente
docker images
# Deve aparecer algo como: bff-rent-car-local


# 4. Marque (tagueie) a imagem para o ACR com nome completo e versão
docker tag bff-rent-car-local acrlab07.azurecr.io/bff-rent-car-local:v1


# 5. Faça o push da imagem para o ACR
docker push acrlab07.azurecr.io/bff-rent-car-local:v1

# 6. Crie o ambiente para Container Apps (apenas uma vez)
az containerapp env create `
  --name bff-rent-car-local `
  --resource-group LAB07 `
  --location eastus

  # Se der erro, use:
# az containerapp env create --name bff-rent-car-local --resource-group LAB07 --location eastus


# 7. Faça o deploy do container usando a imagem do ACR
# ⚠️ Substitua os valores de username e password pelos da sua conta
# Vá em: Azure Portal > ACR > Configurações > Chaves de acesso > Ative "Usuário administrador"
az containerapp create `
  --name bff-rent-car-local `
  --resource-group LAB07 `
  --environment bff-rent-car-local `
  --image acrlab07.azurecr.io/bff-rent-car-local:v1 `
  --target-port 3001 `
  --ingress external `
  --registry-server acrlab07.azurecr.io `
  --registry-username SEU_USERNAME_AQUI `
  --registry-password SUA_PASSWORD_AQUI

# Se der erro, use:
# az containerapp create --name bff-rent-car-local --resource-group LAB07 --environment bff-rent-car-local --image acrlab07.azurecr.io/bff-rent-car-local:v1 --target-port 3001 --ingress 'external' --registry-server acrlab07.azurecr.io --registry-username SEU_USERNAME_AQUI --registry-password SUA_PASSWORD_AQUI


# 8. Observação importante:
# Este comando de criação (az containerapp create) só precisa ser executado uma vez para criar o Container App.
# Se você fizer alterações na imagem (ex: novo código e novo push), não é necessário recriar o container.
# Basta atualizar a imagem com o comando abaixo:

# az containerapp update \
#   --name bff-rent-car-local \
#   --resource-group LAB07 \
#   --image acrlab07.azurecr.io/bff-rent-car-local:v1

# Assim, o Container App continuará usando o mesmo nome, porta e configurações, apenas com a nova imagem.
