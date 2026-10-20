# Plano técnico

## Contexto
Implementa a especificação do encurtador de links, respeitando a constituição do projeto.

## Arquitetura
- API HTTP em Minimal API, um arquivo de endpoints por recurso
- Camada de aplicação com LinkService, dona das regras RN-01 a RN-06
- Camada de dados com EF Core e um repositório por agregado
- Endpoint só valida entrada, chama o serviço e traduz o resultado em resposta
- Toda regra de negócio deve ter teste unitário; todo endpoint deve ter teste de integração de caminho feliz, conforme a Constituição

### Organização por responsabilidade
Manter os projetos atuais e organizar os componentes dentro deles por responsabilidade, sem criar projetos, camadas ou abstrações que não atendam a uma necessidade concreta.

- `LinkShortener.Api`
  - `Endpoints/`: binding HTTP, chamada aos serviços de aplicação e tradução de resultados para respostas HTTP; não contém regras de negócio nem acesso direto a banco.
  - `Contracts/`: DTOs de entrada e saída próprios do protocolo HTTP.
  - `Program.cs`: composição da aplicação e mapeamento dos endpoints; delega o registro e a inicialização da infraestrutura a métodos fornecidos por Infrastructure.
- `LinkShortener.Application`
  - `Domain/Links/`: entidade `Link` e regras puras do domínio, sem dependência de API, EF Core ou Infrastructure. Subpastas devem separar regras e tipos de resultado somente quando isso melhorar a localização dos componentes.
  - `Links/Services/`: serviços que coordenam casos de uso e validadores que dependem de portas ou integrações.
  - `Links/Contracts/`: contratos de fronteira necessários para substituir integrações ou isolar testes, incluindo os contratos de repositório e acessibilidade de URL.
  - `Links/Results/`: resultados dos casos de uso consumidos pela API.
- `LinkShortener.Infrastructure`
  - `Persistence/`: DbContext, mapeamentos, migrações e implementação de repositório.
  - `Links/`: integrações externas usadas pela aplicação e suas implementações.
  - Ponto de composição: registra os serviços concretos, configura SQLite e encapsula a aplicação de migrações de inicialização, preservando o comportamento atual sem expor esses detalhes em `Program.cs`.
- `LinkShortener.Tests`
  - Organizar testes por tipo e responsabilidade (unidade de domínio/aplicação, integração HTTP e persistência), sem exigir projetos de teste adicionais.

As localizações acima definem responsabilidades, não exigem a criação de cada subpasta se não houver componentes para ela. Durante a migração, atualizar namespaces e referências junto com cada grupo movido.

### Regras de dependência e abstrações
- `Domain` não depende de frameworks de aplicação, HTTP, persistência ou serviços externos.
- `Application` pode depender de `Domain`, mas não de API, Infrastructure, EF Core ou SQLite.
- `Infrastructure` implementa contratos da aplicação e pode depender de Application e dos provedores/frameworks necessários à persistência e às integrações.
- API pode depender de Application para atender requisições e de Infrastructure somente no ponto de composição da aplicação. Handlers de endpoint dependem de serviços/contratos da aplicação, nunca de implementações concretas de persistência.
- Testes podem referenciar os projetos necessários para testar o comportamento integrado.
- Criar uma interface somente quando existir uma fronteira cuja implementação precise ser substituível ou isolada em testes. Não criar interfaces automaticamente para cada classe ou serviço.
- Não adicionar testes ou ferramentas de conformidade arquitetural dedicados nesta refatoração; validar os limites por revisão das referências entre projetos, build e testes existentes.
- A refatoração é estrutural: preservar contratos HTTP, regras, persistência e comportamento. Uma funcionalidade não implementada continua fora do escopo e não deve ser criada como parte da reorganização.

## Decisões
- D-01: SQLite como armazenamento, porque RF-08 exige persistência entre reinícios
- D-02: contador incrementado por UPDATE atômico no banco, e não lido e reescrito na aplicação, para atender RN-04 sob acesso concorrente
- D-03: usar exclusivamente o alfabeto A-Z maiúsculo e os dígitos 1-9 para códigos automáticos e personalizados, conforme RF-02, RF-03 e RN-05; não há exclusão adicional de caracteres dentro desse conjunto
- D-04: unicidade do código garantida por índice único no banco, além da checagem no serviço, porque a checagem sozinha tem corrida
- D-05: o endpoint protegido RF-09 usa uma chave aleatória de gerenciamento por link, gerada com 256 bits de entropia e codificada em Base64URL sem padding. A chave é devolvida uma única vez na criação e armazenada somente como hash SHA-256; nunca persistir a chave em texto puro. O endpoint público de RF-07 não exige essa chave.
- D-06: manter T-10 (cache em memória dos links mais acessados) como otimização candidata. A implementação fica condicionada à definição de uma métrica, de uma linha de base e de um alvo de desempenho verificável na tarefa correspondente; não presumir benefício sem medição

## Modelo de dados
Link:
- Id, Code (único), TargetUrl, CreatedAtUtc, ExpiresAtUtc (nulo), AccessCount
- ManagementKeyHash é o hash SHA-256 da chave de gerenciamento por link, incluído para suportar o contrato aprovado de RF-09; a chave em texto puro nunca é persistida

## Contratos
- POST /api/links entrada: targetUrl, code opcional, expiresAtUtc opcional
- POST /api/links saída: shortUrl, code e managementKey em texto puro somente nesta resposta de criação
- GET /{code} redireciona com 302, ou 404 se não existe, ou 410 se expirou
- GET /api/links/{code}/stats retorna publicamente o total de acessos, sem exigir chave, conforme RF-07
- GET /api/links/{code}/details retorna targetUrl, createdAtUtc e expiresAtUtc; exige Authorization: Bearer <chave> com a chave de gerenciamento do link. Retorna 200 para chave válida, 401 para chave ausente ou inválida e 404 para código inexistente. Metadados de links expirados continuam consultáveis com chave válida.

## Riscos
- Concorrência na criação de código próprio, mitigada por D-04
- Crescimento da tabela de links sem política de limpeza, fora de escopo por ora
- Cache pode adicionar complexidade e inconsistência; avaliar por medição antes de implementar
- A chave de gerenciamento é um segredo de alta entropia retornado uma única vez; não é possível recuperá-la após a criação, apenas emitir uma nova chave por fluxo explicitamente aprovado
