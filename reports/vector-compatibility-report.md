# SW360 Compatibility Report

## Application

vector

## Repository and Branch

https://siemens.ghe.com/portfolio/acdc-tc-vector (main)

## SW360 Version

20.2.432

## Overall Risk

LOW. No breaking CompatibilityIssue findings were identified.

## Summary

| Metric | Count |
|---|---:|
| totalApisUsedByApplication | 10 |
| compatibleApis | 0 |
| changedApis | 10 |
| missingApis | 0 |
| compatibilityIssues | 0 |
| applicationContractMismatches | 3 |
| documentationGaps | 7 |
| indeterminateApis | 0 |
| criticalApis | 0 |
| highApis | 0 |
| mediumApis | 10 |
| lowApis | 0 |

## Compatibility Issues

None detected.

## Application Contract Mismatches

### GET:/components/{id}

The connector deserializes component fields at the response root, while OpenAPI references EntityModelComponent with a content wrapper.

Affected files: Vector/Sw360.Connector/Api/Sw360Client.cs

### GET:/releases/{id}

The connector deserializes release fields at the response root, while OpenAPI references EntityModelRelease with a content wrapper.

Affected files: Vector/Sw360.Connector/Api/Sw360Client.cs

### GET:/projects/{id}

The connector deserializes project fields at the response root, while OpenAPI references EntityModelProject with a content wrapper.

Affected files: Vector/Sw360.Connector/Api/Sw360Client.cs


## Documentation Gaps

### GET:/components

The connector consumes page metadata and a namespaced HAL component collection, while the OpenAPI collection response does not describe those fields.

Affected files: Vector/Sw360.Connector/Api/Sw360Client.cs

### GET:/components?name={name}

The connector consumes page metadata and a namespaced HAL component collection, while the OpenAPI collection response does not describe those fields.

Affected files: Vector/Sw360.Connector/Api/Sw360Client.cs

### GET:/releases

The connector consumes a paginated HAL release collection, but OpenAPI documents the 200 response only as an object without properties.

Affected files: Vector/Sw360.Connector/Api/Sw360Client.cs

### GET:/releases?name={name}

The connector consumes a paginated HAL release collection, but OpenAPI documents the 200 response only as an object without properties.

Affected files: Vector/Sw360.Connector/Api/Sw360Client.cs

### GET:/projects

The connector consumes page metadata and a namespaced HAL project collection that the OpenAPI collection schema does not describe.

Affected files: Vector/Sw360.Connector/Api/Sw360Client.cs

### GET:/projects?{filters}

The connector consumes page metadata and a namespaced HAL project collection that the OpenAPI collection schema does not describe.

Affected files: Vector/Sw360.Connector/Api/Sw360Client.cs, Vector/Vector.Application/Services/Sw360ComplianceService.cs

### GET:/projects?name={name}

The connector consumes page metadata and a namespaced HAL project collection that the OpenAPI collection schema does not describe.

Affected files: Vector/Sw360.Connector/Api/Sw360Client.cs


## Informational Findings

None detected.

## Indeterminate APIs

None detected.

## Compatible APIs

None.
