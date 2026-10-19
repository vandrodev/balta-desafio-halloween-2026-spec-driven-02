# Matriz de rastreabilidade de testes

Matriz da T-13 para as regras de negócio RN-01 a RN-06 e os endpoints HTTP atualmente implementados. Os testes unitários verificam decisões da aplicação e políticas; os testes de integração verificam o comportamento HTTP e a persistência real em SQLite.

## Regras de negócio

| Regra | Cobertura unitária | Cobertura adicional |
|---|---|---|
| RN-01 — código único em todo o sistema | `LinkServiceTests.CreateAsync_RejectsCustomCodeAlreadyInUse` | `CreateLinkEndpointTests.PostLinks_WithDuplicateCustomCode_ReturnsConflict` e `LinkPersistenceTests.Migrate_CreatesLinksTableAndUniqueCodeIndex` |
| RN-02 — URL http/https publicamente acessível | `TargetUrlValidatorTests.ValidateAsync_AcceptsAccessibleHttpAndHttpsUrls`, `ValidateAsync_RejectsUnsupportedSchemes`, `ValidateAsync_RejectsMissingMalformedOrCredentialedUrls` e `ValidateAsync_RejectsUrlWhenAccessibilityCheckFails` | `TargetUrlValidatorTests.AccessibilityChecker_SendsHeadForPublicAddress`, `AccessibilityChecker_RejectsPrivateResolvedAddressWithoutRequestingIt` e `AccessibilityChecker_RejectsUnsuccessfulHeadResponses` |
| RN-03 — link expirado não redireciona nem conta acesso | `LinkServiceTests.GetRedirectAsync_DoesNotRedirectOrIncrementExpiredLink` e `LinkExpirationPolicyTests.Validate_UsesLastInstantOfRequestedUtcDateAsInclusiveExpiry` | `LinkRedirectEndpointTests.GetCode_RedirectsExistingLinkAndReturnsNotFoundOrGoneAsAppropriate` e `LinkExpirationEndpointTests.PostLink_RejectsPastUtcDateAndExpiresAtEndOfRequestedUtcDay` |
| RN-04 — contador nunca diminui | `LinkServiceTests.GetRedirectAsync_OnlyIncrementsAccessCountForValidAccesses` | `LinkRedirectEndpointTests.GetCode_RedirectsExistingLinkAndReturnsNotFoundOrGoneAsAppropriate` verifica incremento concorrente e zero incremento para link expirado |
| RN-05 — caracteres e comprimentos permitidos para códigos | `LinkCodeValidatorTests.Validate_AcceptsCodesWithinLengthAndAlphabet`, `Validate_RejectsCodesOutsideLengthRange` e `Validate_RejectsCharactersOutsideAllowedAlphabet` | `CreateLinkEndpointTests.PostLinks_WithInvalidCustomCode_ReturnsBadRequest` |
| RN-06 — código automático em colisão gera nova tentativa | `LinkCodeGeneratorTests.GenerateUniqueAsync_RetriesWhenGeneratedCodeAlreadyExists` | `CreateLinkEndpointTests.PostLinks_WithAccessibleTarget_ReturnsCreatedLink` |

## Endpoints implementados

| Endpoint | Teste de integração do caminho feliz | Casos adicionais |
|---|---|---|
| `POST /api/links` | `CreateLinkEndpointTests.PostLinks_WithAccessibleTarget_ReturnsCreatedLink` | `CreateLinkEndpointTests.PostLinks_WithCustomCode_ReturnsCreatedLinkAndPersistsCode`, `PostLinks_WithDuplicateCustomCode_ReturnsConflict` e `LinkExpirationEndpointTests.PostLink_RejectsPastUtcDateAndExpiresAtEndOfRequestedUtcDay` |
| `GET /{code}` | `LinkRedirectEndpointTests.GetCode_RedirectsExistingLinkAndReturnsNotFoundOrGoneAsAppropriate` | `LinkExpirationEndpointTests.PostLink_RejectsPastUtcDateAndExpiresAtEndOfRequestedUtcDay` cobre o limite final UTC; o teste de redirect cobre 404, 410 e concorrência |
| `GET /api/links/{code}/stats` | `LinkStatisticsEndpointTests.GetStatistics_ReturnsPublicAccessCountWithoutAuthentication` | `LinkStatisticsEndpointTests.GetStatistics_ReturnsNotFoundForUnknownCode` |

O endpoint `GET /api/links/{code}/details` está especificado no contrato de RF-09, mas ainda não está implementado; portanto, não faz parte do inventário de endpoints implementados nem dos critérios de cobertura da T-13.
