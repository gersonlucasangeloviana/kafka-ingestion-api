# Publicação no Dokploy

Ambiente alvo informado: **8 vCPU, 14 GB de RAM, 80 GB de disco**. O tipo de CPU/disco, os limites dos containers e os outros serviços da VPS devem ser registrados na medição. O deploy será configurado pelo proprietário a partir de commits na branch `main`.

## API e Kafka juntos

1. Crie um projeto e um serviço do tipo **Docker Compose**, com provider GitHub/Git.
2. Selecione o repositório `gersonlucasangeloviana/kafka-ingestion-api`, branch `main`, arquivo `compose.yaml`.
3. Em Environment, copie as variáveis do seu `.env.local` sem enviá-las ao GitHub. As duas chaves precisam ser diferentes, com pelo menos 16 caracteres. O Compose injeta explicitamente essas variáveis no container da API.
4. Adicione um domínio ao serviço `api`, porta interna **8080**, com HTTPS. O Dokploy configura o roteamento do Traefik. Não publique o Kafka na internet.
5. Faça o deploy. O Kafka usa um volume persistente; a API espera o health check do broker e cria o tópico no startup. Verifique `/health/ready`.
6. Execute k6 em outra máquina, com `BASE_URL=https://seu-dominio` e a chave de publicação. A chave administrativa só é necessária para limpar os registros.

O `compose.local.yaml` é exclusivo para testes locais. Não o adicione no Dokploy. Use o modo Compose: o modo Stack não aceita build direto do Dockerfile.

O Kafka está configurado com 512 MiB de heap, mas usa memória adicional fora do heap e cache do sistema. Configure os limites de recursos conforme a VPS e registre-os nos resultados. Outros serviços na VPS influenciam a capacidade medida.

## Kafka existente

Crie um serviço Application pelo Dockerfile da raiz, na porta 8080, e configure:

```dotenv
ASPNETCORE_ENVIRONMENT=Production
Authentication__ApiKey=<chave-de-publicacao>
Authentication__AdminApiKey=<outra-chave-administrativa>
Kafka__BootstrapServers=<endereco-interno-do-broker>:9092
Kafka__Topic=benchmark.ids
Kafka__CreateTopicOnStartup=false
Kafka__EnablePurge=false
```

Crie previamente o tópico ou habilite `Kafka__CreateTopicOnStartup=true` com permissão de criação. Todos os `advertised.listeners` retornados pelo Kafka precisam ser alcançáveis a partir do container da API. Configure a rede interna do Dokploy para conectar os serviços.

Para brokers com autenticação/TLS, as opções disponíveis são `Kafka__SecurityProtocol`, `Kafka__SaslMechanism`, `Kafka__SaslUsername`, `Kafka__SaslPassword` e `Kafka__SslCaLocation`. Exemplo: `SaslSsl` e `Plain`. Certificados devem estar montados no caminho informado. As credenciais do Kafka e da API são independentes.

## Limpeza entre experimentos

Habilite `Kafka__EnablePurge=true` apenas para o tópico de testes. Pare o gerador e quaisquer outros produtores, espere as requisições pendentes terminarem e chame o DELETE com a chave administrativa e `X-Confirm-Topic`. A resposta lista os novos low watermarks por partição. O tópico preserva os offsets históricos e a liberação de disco pode demorar.

Não automatizamos limpeza entre etapas de carga para não misturar o custo de exclusão com o desempenho de publicação. Se a exclusão falhar, ela pode ter sido aplicada em parte das partições.

## Arquivos secretos

O `.env.local` original foi gerado com duas chaves fixas aleatórias e permissão de leitura/escrita apenas pelo usuário local. Esse arquivo não vai para o Git nem para a imagem. No Dokploy, as chaves são configuração de runtime. Nunca coloque chaves no Dockerfile, README, relatório de benchmark ou variáveis de build.

Referências: [Compose](https://docs.dokploy.com/docs/core/docker-compose), [domínios](https://docs.dokploy.com/docs/core/docker-compose/domains).
