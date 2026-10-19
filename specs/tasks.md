# Tarefas

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-01 | Criar projeto e estrutura de pastas | Constituição: stack e arquitetura | | Projeto compila |
| T-02 | Modelar a entidade Link e a migração | RF-01, RF-08 | T-01 | Migração aplica no banco |
| T-03 | Gerar código aleatório de 7 caracteres | RF-02, RN-05, RN-06, D-03 | T-01 | Teste unitário cobre comprimento, alfabeto A-Z/1-9 e nova tentativa após colisão |
| T-04 | Validar URL de destino e limite de 2048 caracteres | RN-02, caso de borda da URL de 2048 caracteres | T-01 | Testes cobrem http, https, esquema inválido, comprimento máximo e validação de acessibilidade pública conforme especificação |
| T-05 | Criar link com código automático | RF-01 | T-02, T-03, T-04 | Teste de integração cria e retorna 201 |
| T-06 | Aceitar código próprio com unicidade e validação completa | RF-03, RN-01, RN-05, D-04 | T-02, T-04 | Testes cobrem comprimento de 4 a 20, caracteres permitidos/proibidos e código duplicado retorna 409 |
| T-07 | Redirecionar por código, tratando código inexistente e expiração | RF-04, RN-03 | T-05, T-12 | Testes de integração cobrem 302 para link válido, 404 para inexistente e 410 para expirado |
| T-08 | Incrementar contador de forma atômica | RF-05, RN-04, D-02 | T-07 | Teste concorrente não perde contagem; link expirado não incrementa contador |
| T-09 | Criar endpoint público de estatísticas | RF-07 | T-05, T-08 | Qualquer pessoa que conheça o código consulta o total sem chave; teste de integração cobre caminho feliz |
| T-10 | Avaliar e, se justificado, implementar cache em memória dos links mais acessados | D-06 | T-07, T-08 | Antes da implementação, registrar métrica, linha de base e alvo verificável; implementar apenas se a medição justificar e adicionar testes para consistência do contador e invalidação/expiração |
| T-11 | Definir e aprovar o contrato do endpoint protegido de informações adicionais | RF-09, D-05 | T-09 | Contrato aprovado: GET /api/links/{code}/details retorna targetUrl, createdAtUtc e expiresAtUtc; exige chave aleatória por link no Bearer, devolvida uma única vez na criação e armazenada somente como hash SHA-256; responde 401 para chave ausente/inválida, 404 para código inexistente e permite metadados de links expirados |
| T-12 | Validar data de expiração e limites de validade em UTC | RF-06, RN-03, caso de borda da data no passado | T-02 | Testes cobrem data passada, link válido até o fim da data em UTC e expiração sem redirecionamento nem incremento do contador |
| T-13 | Mapear regras de negócio e endpoints aos testes obrigatórios | Constituição: Qualidade | T-03, T-04, T-05, T-06, T-07, T-08, T-09, T-12 | Matriz de rastreabilidade demonstra teste unitário para cada regra de negócio e teste de integração de caminho feliz para cada endpoint implementado |
