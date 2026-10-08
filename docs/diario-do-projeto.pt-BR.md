# Diário do projeto — Kafka Ingestion API

Registro iniciado em **07/10/2026**, horário de São Paulo. Objetivo: manter o histórico técnico, as decisões e as evidências para preparar um post no LinkedIn e um vídeo no YouTube.

Cada entrada deve indicar **o que foi feito, onde, por quê e como foi validado**. Configurações informadas pelo proprietário são registradas como relato; uma recomendação só passa a ser configuração aplicada depois de confirmada. Datas abaixo indicam a data do registro, não necessariamente a data de instalação dos serviços.

## Ambiente informado em 07/10/2026

| Item | Configuração | Onde foi feito | Situação / evidência |
|---|---|---|---|
| VPS | Kronichost, 8 vCPU, 14 GB de RAM, 80 GB de NVMe | Painel da Kronichost / servidor contratado | Informado pelo proprietário; modelo de CPU, SO e região ainda não registrados |
| Domínio | `vianadev.com.br`, já comprado anteriormente | Provedor de registro não informado | Domínio existente reutilizado no projeto |
| Gestão de DNS | Cloudflare | Zona `vianadev.com.br` no painel da Cloudflare | Informado pelo proprietário; Cloudflare como gestora de DNS não implica transferência do registro do domínio |
| Endereço da API | `api-kafka.vianadev.com.br` | Cloudflare → DNS → registro `api-kafka` | Adição em andamento; tipo, destino, proxy e resolução ainda não validados |
| Observabilidade do servidor | Agente de infraestrutura New Relic configurado para obter logs e métricas | VPS e conta New Relic | Configuração informada pelo proprietário; recebimento de métricas e fontes de logs ainda precisa de evidência |
| Publicação da aplicação | Fluxo previsto via Dokploy, serviço `api` na porta interna 8080, roteamento Traefik | Dokploy / [guia de publicação](dokploy.pt-BR.md) | Preparado no repositório; esta entrada não confirma deploy na VPS |

Os caminhos de instalação/configuração do agente New Relic e as fontes de logs não foram informados. Registrar esses caminhos quando forem consultados. A configuração do agente de infraestrutura não comprova instrumentação APM da API nem métricas específicas do Kafka.

## Histórico já disponível no repositório

| Registro | O que foi feito e motivo | Onde | Evidência |
|---|---|---|---|
| Base da aplicação, commit `55464d3` | API .NET 10 publica IDs no Kafka e só retorna sucesso depois da confirmação do broker; produtor compartilhado e limites de concorrência | `src/KafkaIngestion.Api/`, [arquitetura](architecture.md) | Código, testes e documentação no repositório |
| Execução local e deploy | Docker Compose reúne API e Kafka; configuração local usa portas de loopback; Kafka do deploy fica na rede interna | `docker-compose.yml`, `compose.local.yaml`, [guia Dokploy](dokploy.pt-BR.md) | Arquivos versionados; não confirma instalação remota |
| Experimento de carga | k6 aumenta a carga por etapas e interrompe quando falham critérios de erro, latência ou confirmação | `load-tests/ingestion.js`, `scripts/run-load-tests.sh`, [protocolo](performance.md) | Cenário e gerador de relatório versionados |
| Validação local, registrada em 07/10/2026, commit `f7c765c` | Etapas de 10 segundos até 5.000 req/s; última etapa com 50.001 publicações confirmadas e zero erros/dropped iterations | Máquina local, API e Kafka em Docker | [Relatório local](local-smoke-test.md); não representa capacidade sustentada da VPS |
| Ajuste do deploy, commit `80c5a00` | Alinhamento do nome do Compose com o caminho utilizado no Dokploy | `docker-compose.yml`, [guia Dokploy](dokploy.pt-BR.md) | Histórico Git |
| Documentação, 07/10/2026 | Criação deste diário e orientação para comparar carga com e sem proxy Cloudflare | `docs/` e índice no `README.md` | Arquivos de documentação; nenhuma configuração externa alterada nesta etapa |

## Cloudflare e testes de carga — orientação registrada em 07/10/2026

**Recomendação, ainda não aplicada:** medir primeiro o caminho direto até a VPS e depois repetir pelo domínio com proxy ativo. O primeiro mede API + Kafka + proxy da VPS/TLS/rede, quando presentes; o segundo acrescenta a borda da Cloudflare. Ambos precisam ter o caminho identificado no relatório.

| Cenário | Caminho | Configuração sugerida |
|---|---|---|
| Benchmark inicial da VPS | Gerador externo → VPS/Traefik → API → Kafka | `api-kafka` em **DNS only** durante uma janela controlada, ou sobrescrita de DNS apenas no gerador para o IP da VPS, preservando hostname e TLS |
| Teste pelo endereço público | Gerador externo → Cloudflare → VPS/Traefik → API → Kafka | Proxy ativo; registrar regras de segurança e eventuais bloqueios |
| Uso normal da API HTTP/HTTPS | Cliente → Cloudflare → VPS → API → Kafka | Proxy ativo, HTTPS na porta 443 e SSL/TLS **Full (strict)** com certificado válido na origem |

### Cuidados que afetam a validade da medição

- WAF, rate limiting, proteção contra bots e mitigação de DDoS podem interferir em tráfego automatizado. Não há nesta documentação um número universal de req/s que garanta passagem pela Cloudflare.
- Uma exceção temporária, quando necessária e disponível no plano, deve combinar hostname, IP público do gerador e rotas do teste (`/api/v1/messages` e `/health/ready`), preservando autenticação da API. Registrar a regra e removê-la ao terminar. Não presumir que uma exceção de WAF desativa todas as proteções.
- **Bot Fight Mode não pode ser ignorado por uma regra WAF com ação Skip.** Super Bot Fight Mode permite Skip. Verificar qual produto está ativo antes de planejar exceções.
- DNS only revela o IP da origem e retira as proteções do proxy naquele hostname; o firewall precisa permitir o gerador. Evitar alterar a zona inteira ou outros subdomínios do domínio existente.
- Antes de acesso direto, confirmar HTTPS válido na VPS para `api-kafka.vianadev.com.br`. Um certificado apenas da **Cloudflare Origin CA** não é confiável por padrão para clientes diretos; para esse caminho, preferir certificado de uma autoridade pública, como Let's Encrypt. Preservar hostname/SNI; não desativar a validação TLS para fazer o teste passar.
- Após trocar o proxy, aguardar os caches DNS e verificar o caminho usado pelo gerador. Uma troca no painel não garante que o cliente já esteja acessando a origem.
- Um `429` pode vir da API ou da Cloudflare. Correlacionar resposta, horário, logs da API/Traefik e eventos de segurança da Cloudflare. O header `CF-Ray` ajuda na investigação, mas sua presença sozinha não identifica quem gerou o erro.
- Manter o Kafka na rede interna. O proxy HTTP/HTTPS comum da Cloudflare não transporta o protocolo nativo do broker.

Referências oficiais consultadas: [proxy e DNS only](https://developers.cloudflare.com/dns/proxy-status/), [uso do proxy para APIs](https://developers.cloudflare.com/dns/proxy-status/use-cases/), [opções de Skip](https://developers.cloudflare.com/waf/custom-rules/skip/options/), [Bot Fight Mode](https://developers.cloudflare.com/bots/get-started/bot-fight-mode/), [respostas do rate limiting](https://developers.cloudflare.com/waf/rate-limiting-rules/parameters/), [Full (strict)](https://developers.cloudflare.com/ssl/origin-configuration/ssl-modes/full-strict/), [Origin CA e acesso direto](https://developers.cloudflare.com/ssl/origin-configuration/origin-ca/), [limitações de protocolos](https://developers.cloudflare.com/dns/proxy-status/limitations/).

## Evidências a registrar antes e durante a medição na VPS

- DNS concluído: tipo do registro, destino em anotação privada, estado do proxy e data/hora da validação.
- Deploy: commit/imagens publicados, configuração do domínio no Dokploy, certificado TLS e resultado de `/health/ready` e de uma publicação autenticada.
- New Relic: host identificado, métricas chegando, fontes de logs efetivamente recebidas e caminhos dos arquivos de configuração. Registrar separadamente integrações de containers, Kafka ou APM, se forem adicionadas.
- Ambiente: modelo da CPU, SO, região, limites dos containers, serviços concorrentes e recursos/localização do gerador externo.
- Cada execução: data/hora/fuso, identificador, commit, caminho de rede, regras Cloudflare, taxa alvo e taxa observada, confirmações, erros, p95/p99 e dropped iterations.
- Recursos no mesmo intervalo: CPU, RAM, disco/I/O e rede; coletar métricas específicas da API/Kafka apenas quando houver instrumentação validada.
- Evidências: relatórios brutos em `artifacts/load-tests/<RUN_ID>/`, resumo revisado em `docs/`, capturas de gráficos e eventuais falhas com diagnóstico. Os artefatos brutos são ignorados pelo Git; preservá-los em local privado para a análise posterior.

## Material para LinkedIn e YouTube

Linha narrativa a desenvolver quando houver resultados: problema de ingestão → API e confirmação Kafka → infraestrutura Kronichost → domínio/DNS/HTTPS → observabilidade New Relic → desenho do teste → comparação com e sem Cloudflare → gargalos, ajustes e aprendizados.

Capturas úteis: arquitetura, recursos da VPS, registro DNS, domínio do serviço no Dokploy, confirmação de uma publicação, execução do k6 e gráficos New Relic do mesmo intervalo. Preparar versões sem chaves de API, tokens, dados de conta ou outros segredos antes de publicar.

Estado atual para comunicação: há uma validação funcional local documentada. **Ainda não há resultado de capacidade sustentada da VPS.** Usar os números do relatório local com duração e ambiente explícitos; preencher os resultados remotos só depois das medições.

## Modelo para as próximas entradas

### AAAA-MM-DD — título da etapa

- **O que fizemos:** alteração concreta.
- **Onde fizemos:** serviço/painel e caminho, arquivo ou commit.
- **Por quê:** problema ou objetivo.
- **Validação:** procedimento e resultado observado, com horário/fuso.
- **Evidência:** relatório, captura revisada ou link sem segredos.
- **Pendências/aprendizado:** próximo passo e conclusão sustentada pela evidência.
