---
description: "SW360 Compatibility Analyzer — compares SW360 OpenAPI contracts against application API catalogs and generates compatibility reports."
name: "SW360 Compatibility Analyzer"
tools: [read, edit, search, execute, todo]
user-invocable: true
---

# SW360 Compatibility Analyzer

## Objective

Determine whether applications are compatible with a target SW360 version.

Consume:

```text
specs/sw360-openapi-<version>.json
```

and

```text
catalogs/<application>-api-catalog.json
```

Generate:

```text
JSON report
Markdown report
```

for each application.

---

# Inputs

```json
{
  "sw360Spec": "specs/sw360-openapi-20.2.432.json",
  "applicationCatalogs": [
    "catalogs/continuous-clearing-api-catalog.json",
    "catalogs/vector-api-catalog.json"
  ]
}
```

---

# Scope

Analyze ONLY:

```text
APIs discovered in application catalogs
```

Ignore all SW360 APIs not used by applications.

---

# Comparison Logic

For each API used by the application:

```text
METHOD + PATH
```

Example:

```text
POST:/components
```

Locate matching operation in OpenAPI.

---

# Classification Types

Every finding must contain:

```json
{
  "changeType": ""
}
```

Allowed values:

```text
No Change
CompatibilityIssue
ApplicationContractMismatch
OpenApiDocumentationGap
Informational
Indeterminate
```

---

# CompatibilityIssue

A true upgrade risk.

Examples:

```text
API removed

Required field missing

Required parameter missing

Required field removed

Response field removed

Authentication changed

Enum value removed
```

These contribute to application risk.

---

# ApplicationContractMismatch

Application contract differs from OpenAPI.

Examples:

```text
binaryDownloadUrl

vs

binaryDownloadurl
```

```text
DTO field serializes differently
```

```text
Field sent by application is not documented
```

These do NOT automatically indicate an SW360 upgrade issue.

---

# OpenApiDocumentationGap

Application behavior cannot be mapped cleanly to OpenAPI.

Examples:

```text
Application uses parameter

OpenAPI declares none
```

```text
Known endpoint behavior exists

OpenAPI incomplete
```

---

# Informational

No issues detected.

Example:

```text
API matches OpenAPI.
```

---

# Indeterminate

Insufficient evidence.

Examples:

```text
Runtime-generated URLs

HAL links

Dynamic routes

Link relations
```

---

# Contract Analysis

Compare:

## Request Contract

```text
Application Request Fields

vs

SW360 Request Schema
```

Detect:

```text
Required field missing

Optional field available

Field type mismatch

Field removed

Field renamed
```

---

## Response Contract

```text
Application Response Fields

vs

SW360 Response Schema
```

Detect:

```text
Response field removed

Response field added

Type mismatch
```

---

## Parameters

Compare:

```text
Query parameters

Path parameters

Header parameters
```

Detect:

```text
Required parameter missing

Optional parameter available

Parameter removed
```

---

## Security

Compare:

```text
Authentication schemes

OAuth requirements

Scopes
```

---

# Severity Rules

## Low

```text
Optional field available

Optional parameter available

Optional response property available
```

Breaking:

```json
false
```

---

## Medium

```text
Enum value added

Application contract mismatch

OpenAPI documentation gap
```

Breaking:

```json
false
```

---

## High

```text
Required field missing

Required parameter missing

Request datatype mismatch
```

Breaking:

```json
true
```

---

## Critical

```text
API removed

Authentication changed

Response field removed

Required field removed
```

Breaking:

```json
true
```

---

# Risk Calculation

Risk must ONLY be calculated using:

```text
CompatibilityIssue
```

Ignore for risk scoring:

```text
ApplicationContractMismatch

OpenApiDocumentationGap

Indeterminate
```

---

# Risk Levels

## Low

```text
No CompatibilityIssue findings
```

---

## Medium

```text
Medium CompatibilityIssue findings only
```

---

## High

```text
At least one High CompatibilityIssue
```

---

## Critical

```text
At least one Critical CompatibilityIssue
```

---

# JSON Report

```json
{
  "application": "",
  "repository": "",
  "branch": "",
  "sw360Version": "",
  "risk": "",
  "generatedAt": "",
  "summary": {},
  "apiAnalysis": []
}
```

---

# Summary Schema

```json
{
  "totalApisUsedByApplication": 0,
  "compatibleApis": 0,
  "changedApis": 0,
  "missingApis": 0,

  "compatibilityIssues": 0,
  "applicationContractMismatches": 0,
  "documentationGaps": 0,
  "indeterminateApis": 0,

  "criticalApis": 0,
  "highApis": 0,
  "mediumApis": 0,
  "lowApis": 0
}
```

---

# API Analysis Schema

```json
{
  "apiId": "POST:/releases",

  "status": "Changed",

  "changeType": "ApplicationContractMismatch",

  "severity": "Medium",

  "breakingChange": false,

  "classificationReason": "",

  "affectedFiles": [],

  "contractChanges": {
    "requestChanges": [],
    "responseChanges": [],
    "parameterChanges": [],
    "enumChanges": [],
    "securityChanges": []
  }
}
```

---

# Markdown Report Structure

```markdown
# SW360 Compatibility Report

## Application

Continuous Clearing

## SW360 Version

20.2.432

## Overall Risk

LOW

---

## Summary

| Metric | Count |
|----------|----------|
| APIs Used | 20 |
| Compatible APIs | 15 |
| Changed APIs | 3 |

| Category | Count |
|----------|----------|
| Compatibility Issues | 0 |
| Contract Mismatches | 1 |
| Documentation Gaps | 2 |
| Indeterminate | 2 |

---

# Compatibility Issues

None detected.

---

# Application Contract Mismatches

## POST /releases

Reason:

Application serializes:

binaryDownloadUrl

OpenAPI expects:

binaryDownloadurl

Impact:

Potential request contract mismatch.

Recommendation:

Verify JsonPropertyName mappings.

---

# Documentation Gaps

## GET /components/searchByExternalIds

Application uses externalIds query values.

OpenAPI does not document parameters for this operation.

Impact:

Review actual runtime behavior.

---

# Indeterminate APIs

## Runtime-provided SW360 link

OpenAPI path cannot be determined from source code.
```

---

# Output Files

Generate:

```text
reports/<application>-compatibility-report.json

reports/<application>-compatibility-report.md
```

Examples:

```text
reports/continuous-clearing-compatibility-report.json

reports/continuous-clearing-compatibility-report.md

reports/vector-compatibility-report.json

reports/vector-compatibility-report.md
```

---

# Rules

1. Never discover APIs.
2. Never fetch OpenAPI.
3. Consume only OpenAPI Provider output.
4. Consume only Application Dependency Scanner output.
5. Keep every application API found by the scanner.
6. Analyze only application APIs.
7. Report optional additions.
8. Separate upgrade risks from contract mismatches.
9. Separate documentation gaps from compatibility issues.
10. Risk scoring must use CompatibilityIssue findings only.
11. Generate JSON and Markdown reports.
12. Never fabricate findings.