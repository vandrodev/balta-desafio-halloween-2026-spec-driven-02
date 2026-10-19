# Encurtador de links

## Problema
Links longos quebram em mensagens, e-mails e materiais impressos, e não dá para medir quantas pessoas clicaram neles.

## Objetivo
Permitir que uma pessoa transforme uma URL longa em um link curto, compartilhe esse link e acompanhe quantos acessos ele recebeu.

## Usuários
- Criador do link: gera links curtos e acompanha os acessos
- Visitante: clica no link curto e chega ao destino

## Histórias
- Como criador, quero encurtar uma URL para compartilhar um link menor
- Como criador, quero escolher o código do link para usar um nome legível
- Como criador, quero ver quantos acessos meu link recebeu
- Como visitante, quero ser redirecionado ao destino sem passar por tela intermediária

## Requisitos funcionais
- RF-01: o sistema recebe uma URL de destino e devolve um link curto
- RF-02: o código do link tem 7 caracteres gerados automaticamente, usando apenas letras maiúsculas de A a Z e números de 1 a 9
- RF-03: o criador pode informar um código próprio de 4 a 20 caracteres, usando apenas letras maiúsculas de A a Z e números de 1 a 9, sem espaços
- RF-04: acessar um link curto existente redireciona para a URL de destino
- RF-05: cada acesso a um link válido incrementa o contador daquele link, incluindo acessos repetidos e automatizados
- RF-06: o criador pode definir uma data de expiração para o link; a data e o limite de validade são interpretados em UTC, e o link permanece válido até o fim da data informada
- RF-07: qualquer pessoa que conheça o código pode consultar o total de acessos do link
- RF-08: os links e seus dados devem ser persistidos em banco de dados para que não se percam após reinício da aplicação
- RF-09: deve existir um endpoint protegido `GET /api/links/{code}/details` para consultar `targetUrl`, `createdAtUtc` e `expiresAtUtc` de um link. O acesso exige a chave de gerenciamento daquele link no cabeçalho `Authorization: Bearer <chave>`. A chave é gerada aleatoriamente com 256 bits de entropia, codificada em Base64URL sem padding, devolvida uma única vez na resposta de criação e armazenada somente como hash SHA-256. Chave ausente ou inválida retorna 401; código inexistente retorna 404. Links expirados continuam permitindo a consulta dos metadados com chave válida.

## Regras de negócio
- RN-01: o código do link é único em todo o sistema
- RN-02: a URL de destino precisa usar http ou https e ser publicamente acessível
- RN-03: link expirado não redireciona e não conta acesso
- RN-04: o contador de acessos nunca diminui
- RN-05: códigos automáticos e personalizados aceitam somente A-Z maiúsculo e os dígitos de 1 a 9; o dígito 0, minúsculas, espaços e caracteres especiais não são permitidos
- RN-06: se um código gerado automaticamente já estiver em uso, o sistema tenta gerar outro código

## Casos de borda
- Código já em uso: recusar a criação e explicar o motivo
- Código gerado automaticamente já em uso: gerar outro código e tentar novamente
- Código inexistente no acesso: responder que o link não existe
- URL de destino com mais de 2048 caracteres: recusar
- URL de destino não publicamente acessível: recusar
- Data de expiração no passado: recusar na criação

## Fora de escopo
- Cadastro e login de usuários
- Domínio personalizado
- Relatório com origem geográfica do acesso
- Edição do destino de um link já criado

## Critérios de aceite
- Criar um link e acessá-lo redireciona para o destino informado
- Dois links não podem ter o mesmo código
- Link com data de expiração vencida responde erro e não incrementa o contador
- Um link ainda válido funciona até o fim de sua data de expiração em UTC
- Qualquer pessoa que conheça o código pode consultar o total de acessos
- Após reiniciar a aplicação, os links persistidos no banco continuam funcionando
- Os códigos automáticos e personalizados aceitam somente caracteres A-Z e 1-9, respeitando os comprimentos definidos em RF-02 e RF-03
- O endpoint protegido de RF-09 retorna `targetUrl`, `createdAtUtc` e `expiresAtUtc`, exige a chave de gerenciamento por link no Bearer, retorna 401 para chave ausente/inválida e 404 para código inexistente, e permite consultar metadados de links expirados com chave válida
