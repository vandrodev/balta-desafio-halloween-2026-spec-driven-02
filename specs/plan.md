# Plano técnico

## Contexto
Implementa a especificação do encurtador de links, respeitando a constituição do projeto.

## Arquitetura
- API HTTP em Minimal API, um arquivo de endpoints por recurso
- Camada de aplicação com LinkService, dona das regras RN-01 a RN-06
- Camada de dados com EF Core e um repositório por agregado
- Endpoint só valida entrada, chama o serviço e traduz o resultado em resposta
- Toda regra de negócio deve ter teste unitário; todo endpoint deve ter teste de integração de caminho feliz, conforme a Constituição

## Decisões
- D-01: SQLite como armazenamento, porque RF-08 exige persistência entre reinícios
- D-02: contador incrementado por UPDATE atômico no banco, e não lido e reescrito na aplicação, para atender RN-04 sob acesso concorrente
- D-03: usar exclusivamente o alfabeto A-Z maiúsculo e os dígitos 1-9 para códigos automáticos e personalizados, conforme RF-02, RF-03 e RN-05; não há exclusão adicional de caracteres dentro desse conjunto
- D-04: unicidade do código garantida por índice único no banco, além da checagem no serviço, porque a checagem sozinha tem corrida
- D-05: caso a chave de gerenciamento de RF-09 seja mantida após a definição do contrato, armazená-la como hash, nunca em texto puro; não exigir essa chave para o endpoint público de total de acessos de RF-07
- D-06: manter T-10 (cache em memória dos links mais acessados) como otimização candidata. A implementação fica condicionada à definição de uma métrica, de uma linha de base e de um alvo de desempenho verificável na tarefa correspondente; não presumir benefício sem medição

## Modelo de dados
Link:
- Id, Code (único), TargetUrl, CreatedAtUtc, ExpiresAtUtc (nulo), AccessCount
- ManagementKeyHash só será incluído se o contrato de RF-09 aprovado exigir chave de gerenciamento

## Contratos
- POST /api/links entrada: targetUrl, code opcional, expiresAtUtc opcional
- POST /api/links saída: shortUrl, code, managementKey apenas se a chave for aprovada e necessária para RF-09
- GET /{code} redireciona com 302, ou 404 se não existe, ou 410 se expirou
- GET /api/links/{code}/stats retorna publicamente o total de acessos, sem exigir chave, conforme RF-07
- Endpoint protegido para informações adicionais: rota, campos de resposta e autenticação pendentes de definição e aprovação em RF-09; não implementar até essa decisão

## Riscos
- Concorrência na criação de código próprio, mitigada por D-04
- Crescimento da tabela de links sem política de limpeza, fora de escopo por ora
- Cache pode adicionar complexidade e inconsistência; avaliar por medição antes de implementar
- O contrato das informações adicionais protegidas permanece pendente até aprovação de RF-09
