# SW360 Compatibility Report

## Application

continuous-clearing

## Repository and Branch

https://github.com/siemens/continuous-clearing (main)

## SW360 Version

20.2.432

## Overall Risk

LOW. No breaking CompatibilityIssue findings were identified.

## Summary

| Metric | Count |
|---|---:|
| totalApisUsedByApplication | 20 |
| compatibleApis | 7 |
| changedApis | 10 |
| missingApis | 0 |
| compatibilityIssues | 0 |
| applicationContractMismatches | 3 |
| documentationGaps | 7 |
| indeterminateApis | 3 |
| criticalApis | 0 |
| highApis | 0 |
| mediumApis | 10 |
| lowApis | 7 |

## Compatibility Issues

None detected.

## Application Contract Mismatches

### GET:/releases/{id}

The application deserializes release fields at the response root, while OpenAPI references EntityModelRelease with a content wrapper.

Affected files: src/SIT.APICommunications/SW360Apicommunication.cs, src/SIT.Services/Sw360CreatorService.cs, src/SIT.Services/Sw360Service.cs

### POST:/releases

The application request DTO includes _links, which is not declared in the OpenAPI ReleaseMixin request schema.

Affected files: src/SIT.APICommunications/SW360Apicommunication.cs, src/SIT.Services/Sw360CreatorService.cs

### PATCH:/releases/{id}

The application request DTO includes _links, which is not declared in the OpenAPI ReleaseMixin request schema.

Affected files: src/SIT.APICommunications/SW360Apicommunication.cs, src/SIT.Services/Sw360CreatorService.cs


## Documentation Gaps

### GET:/projects

The application uses the legacy query parameter _sw360_portlet_projects_STATE; OpenAPI declares state but not this parameter.

Affected files: src/SIT.APICommunications/SW360Apicommunication.cs, src/SIT.Services/Sw360ProjectService.cs, src/SIT.Services/Sw360CreatorService.cs

### GET:/projects/{id}

The application model reads embedded release and linked-release details that are not described by the ProjectMixin response schema.

Affected files: src/SIT.APICommunications/SW360Apicommunication.cs, src/SIT.Services/Sw360ProjectService.cs

### GET:/releases

The 200 response is documented only as an object with no properties, so the cataloged release and attachment response fields cannot be verified.

Affected files: src/SIT.APICommunications/SW360Apicommunication.cs, src/SIT.Services/Sw360CreatorService.cs, src/SIT.Services/Sw360Service.cs

### GET:/releases/searchByExternalIds

The application consumes a paginated HAL release collection, while OpenAPI declares a ReleaseMixin response and omits the collection wrapper.

Affected files: src/SIT.APICommunications/SW360Apicommunication.cs, src/SIT.Services/Sw360CreatorService.cs, src/SIT.Services/Sw360Service.cs

### GET:/components

The application consumes page metadata and a namespaced HAL component collection that the OpenAPI response schema does not describe.

Affected files: src/SIT.APICommunications/SW360Apicommunication.cs, src/SIT.Services/Sw360CreatorService.cs, src/SIT.Services/Sw360Service.cs

### GET:/components/searchByExternalIds

The application sends externalIds as a query parameter, but the operation declares none; its paginated namespaced response wrapper is also undocumented.

Affected files: src/SIT.APICommunications/SW360Apicommunication.cs, src/SIT.Services/Sw360CreatorService.cs

### POST:/projects/{id}/releases

The request body is documented as an unconstrained object; the AddLinkedRelease map value fields releaseRelation and comment are not specified.

Affected files: src/SIT.APICommunications/SW360Apicommunication.cs, src/SIT.Services/Sw360CreatorService.cs


## Informational Findings

None detected.

## Indeterminate APIs

### GET:/users

The application catalog has no typed response DTO or response fields, so its consumed response contract cannot be compared.

Affected files: src/SIT.APICommunications/SW360Apicommunication.cs

### GET:<SW360-provided-link>

The request target is supplied at runtime by SW360 and cannot be resolved to a static OpenAPI operation.

Affected files: src/SIT.APICommunications/SW360Apicommunication.cs, src/SIT.Services/Sw360CreatorService.cs, src/SIT.Services/Sw360Service.cs

### GET:<SW360-attachment-download-link>

The download URL is supplied at runtime; its static operation and response contract cannot be determined.

Affected files: src/SIT.APICommunications/SW360Apicommunication.cs, src/SIT.Services/Sw360Service.cs


## Compatible APIs

- GET:/releases/{id}/triggerFossologyProcess
- GET:/components/{id}
- PATCH:/projects/{id}/release/{releaseId}
- POST:/components
- PATCH:/components/{id}
- PATCH:/projects/{id}
- POST:/releases/{releaseId}/attachments
