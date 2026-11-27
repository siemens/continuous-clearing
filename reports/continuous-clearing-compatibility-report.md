# SW360 Compatibility Report

## Application

Continuous Clearing

## SW360 Release Transition

sw360-20.1.1 to sw360-20.2.0-rc-1

## Risk

MEDIUM

## Summary

| Metric | Count |
|---------|---------|
| APIs Used | 19 |
| APIs With Reported Changes | 10 |
| Added APIs | 0 |
| Modified APIs | 10 |
| Removed APIs | 0 |
| Unchanged APIs | 8 |
| Unresolved APIs | 1 |
| Breaking APIs | 0 |
| Critical APIs | 0 |
| High APIs | 0 |
| Medium APIs | 6 |
| Low APIs | 4 |

## API Analysis

### GET:/resource/api/projects

Status: Modified

Diff API ID: GET:/projects

Severity: Low

Breaking Change: No

Modification Type: Parameter

Description: Runtime OpenAPI contract changed: Parameter.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

Reported Details:

```json
{
  "parameterChanges": [
    {
      "field": "query:searchText",
      "change": "Optional parameter added."
    }
  ]
}
```

---

### GET:/resource/api/users

Status: Modified

Diff API ID: GET:/users

Severity: Low

Breaking Change: No

Modification Type: Parameter

Description: Runtime OpenAPI contract changed: Parameter.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

Reported Details:

```json
{
  "parameterChanges": [
    {
      "field": "query:luceneSearch.default",
      "change": "default changed: unspecified -> true."
    }
  ]
}
```

---

### GET:/resource/api/projects/{id}

Status: Unchanged

Description: No change was reported for this API in the release transition.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

---

### GET:/resource/api/releases/{id}/triggerFossologyProcess

Status: Unchanged

Description: No change was reported for this API in the release transition.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

---

### GET:/resource/api/components

Status: Modified

Diff API ID: GET:/components

Severity: Low

Breaking Change: No

Modification Type: Parameter

Description: Runtime OpenAPI contract changed: Parameter.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

Reported Details:

```json
{
  "parameterChanges": [
    {
      "field": "query:searchText",
      "change": "Optional parameter added."
    }
  ]
}
```

---

### GET:/resource/api/releases/searchByExternalIds

Status: Modified

Diff API ID: GET:/releases/searchByExternalIds

Severity: Medium

Breaking Change: No

Modification Type: Enum

Description: Runtime OpenAPI contract changed: Enum.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

Reported Details:

```json
{
  "enumChanges": [
    {
      "field": "responses.200.application/vnd.hal+json.schema.attachments.items.attachmentType",
      "change": "Enum values added: \"ADS_JSON\"."
    }
  ]
}
```

---

### GET:/resource/api/components/searchByExternalIds

Status: Unchanged

Description: No change was reported for this API in the release transition.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

---

### GET:/resource/api/releases/{id}

Status: Modified

Diff API ID: GET:/releases/{id}

Severity: Medium

Breaking Change: No

Modification Type: Enum

Description: Runtime OpenAPI contract changed: Enum.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

Reported Details:

```json
{
  "enumChanges": [
    {
      "field": "responses.200.application/vnd.hal+json.schema.content.attachments.items.attachmentType",
      "change": "Enum values added: \"ADS_JSON\"."
    }
  ]
}
```

---

### GET:/resource/api/components/{id}

Status: Unchanged

Description: No change was reported for this API in the release transition.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

---

### POST:/resource/api/projects/{id}/releases

Status: Unchanged

Description: No change was reported for this API in the release transition.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

---

### PATCH:/resource/api/projects/{id}/release/{id}

Status: Unchanged

Description: No change was reported for this API in the release transition.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

---

### POST:/resource/api/components

Status: Unchanged

Description: No change was reported for this API in the release transition.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

---

### POST:/resource/api/releases

Status: Modified

Diff API ID: POST:/releases

Severity: Medium

Breaking Change: No

Modification Type: Enum

Description: Runtime OpenAPI contract changed: Enum.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

Reported Details:

```json
{
  "enumChanges": [
    {
      "field": "requestBody.application/json.schema.attachments.items.attachmentType",
      "change": "Enum values added: \"ADS_JSON\"."
    },
    {
      "field": "responses.200.application/vnd.hal+json.schema.content.attachments.items.attachmentType",
      "change": "Enum values added: \"ADS_JSON\"."
    }
  ]
}
```

---

### PATCH:/resource/api/releases/{id}

Status: Modified

Diff API ID: PATCH:/releases/{id}

Severity: Medium

Breaking Change: No

Modification Type: Enum

Description: Runtime OpenAPI contract changed: Enum.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

Reported Details:

```json
{
  "enumChanges": [
    {
      "field": "requestBody.application/json.schema.attachments.items.attachmentType",
      "change": "Enum values added: \"ADS_JSON\"."
    },
    {
      "field": "responses.200.application/vnd.hal+json.schema.content.attachments.items.attachmentType",
      "change": "Enum values added: \"ADS_JSON\"."
    }
  ]
}
```

---

### PATCH:/resource/api/components/{id}

Status: Modified

Diff API ID: PATCH:/components/{id}

Severity: Medium

Breaking Change: No

Modification Type: Enum

Description: Runtime OpenAPI contract changed: Enum.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

Reported Details:

```json
{
  "enumChanges": [
    {
      "field": "requestBody.application/json.schema.attachments.items.attachmentType",
      "change": "Enum values added: \"ADS_JSON\"."
    }
  ]
}
```

---

### PATCH:/resource/api/projects/{id}

Status: Modified

Diff API ID: PATCH:/projects/{id}

Severity: Medium

Breaking Change: No

Modification Type: ResponseSchema

Description: Runtime OpenAPI contract changed: ResponseSchema.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

Reported Details:

```json
{
  "responseChanges": [
    {
      "field": "responses.403",
      "change": "Response status added."
    }
  ]
}
```

---

### POST:/resource/api/releases/{id}/attachments

Status: Unchanged

Description: No change was reported for this API in the release transition.

Affected Files:

- src/SIT.APICommunications/AttachmentHelper.cs

---

### GET:/resource/api/releases

Status: Modified

Diff API ID: GET:/releases

Severity: Low

Breaking Change: No

Modification Type: Parameter

Description: Runtime OpenAPI contract changed: Parameter.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

Reported Details:

```json
{
  "parameterChanges": [
    {
      "field": "query:searchText",
      "change": "Optional parameter added."
    }
  ]
}
```

---

### GET:<SW360-provided-link>

Status: Unresolved

Description: Compatibility could not be determined from the release diff because the catalog path is unknown or a SW360-provided link.

Affected Files:

- src/SIT.APICommunications/SW360Apicommunication.cs

---

