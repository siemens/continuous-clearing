# SW360 Compatibility Report

## Application

Vector

## SW360 Release Transition

sw360-20.1.1 to sw360-20.2.0-rc-1

## Risk

MEDIUM

## Summary

| Metric | Count |
|---------|---------|
| APIs Used | 6 |
| APIs With Reported Changes | 4 |
| Added APIs | 0 |
| Modified APIs | 4 |
| Removed APIs | 0 |
| Unchanged APIs | 2 |
| Unresolved APIs | 0 |
| Breaking APIs | 0 |
| Critical APIs | 0 |
| High APIs | 0 |
| Medium APIs | 1 |
| Low APIs | 3 |

## API Analysis

### GET:/resource/api/components

Status: Modified

Diff API ID: GET:/components

Severity: Low

Breaking Change: No

Modification Type: Parameter

Description: Runtime OpenAPI contract changed: Parameter.

Affected Files:

- Vector/Sw360.Connector/Api/Sw360Client.cs

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

### GET:/resource/api/components/{id}

Status: Unchanged

Description: No change was reported for this API in the release transition.

Affected Files:

- Vector/Sw360.Connector/Api/Sw360Client.cs

---

### GET:/resource/api/releases

Status: Modified

Diff API ID: GET:/releases

Severity: Low

Breaking Change: No

Modification Type: Parameter

Description: Runtime OpenAPI contract changed: Parameter.

Affected Files:

- Vector/Sw360.Connector/Api/Sw360Client.cs

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

### GET:/resource/api/releases/{id}

Status: Modified

Diff API ID: GET:/releases/{id}

Severity: Medium

Breaking Change: No

Modification Type: Enum

Description: Runtime OpenAPI contract changed: Enum.

Affected Files:

- Vector/Sw360.Connector/Api/Sw360Client.cs

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

### GET:/resource/api/projects

Status: Modified

Diff API ID: GET:/projects

Severity: Low

Breaking Change: No

Modification Type: Parameter

Description: Runtime OpenAPI contract changed: Parameter.

Affected Files:

- Vector/Sw360.Connector/Api/Sw360Client.cs

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

### GET:/resource/api/projects/{id}

Status: Unchanged

Description: No change was reported for this API in the release transition.

Affected Files:

- Vector/Sw360.Connector/Api/Sw360Client.cs

---

