SW360 Agent Workflow

Use the agents in this order:

1. **SW360 Release Analyzer** (`sw360-release-analyzer.agent.md`) discovers the newest published SW360 release and its immediate predecessor, obtains or derives exact-release OpenAPI specs, compares API operations, and writes `reports/sw360-api-diff-<latest-release>.json`. It also stores specs under `specs/` and updates `state/release-state.json` only after successful validation. If the newest release was already processed, it exits without regenerating the report.
2. **SW360 Application Dependency Tracker** (`sw360-application-dependency-tracker.agent.md`) scans each requested application and writes successful catalogs to `catalogs/<application>-api-catalog.json`, plus a consolidated execution summary at `registry/api-dependency-catalog.json`. It continues after an individual application fails; check the summary and pass only successful catalog paths to the next step.
3. **SW360 Compatibility Analyzer** (`sw360-compatibility-analyzer.agent.md`) matches each catalog's used APIs against the release diff and writes `reports/<application>-compatibility-report.json` and `.md`.

The compatibility analyzer does not compare releases or read OpenAPI specs. The release diff is its sole source of API changes; it reports only changes matching an application's catalog. `Unchanged` means no change was reported, while `Unresolved` means compatibility could not be determined. Review unresolved entries and source evidence before making upgrade decisions.

## Running the workflow

In a Copilot agent session, reference the corresponding definition under `.github/agents/` and provide each agent with its required input. Ensure repository, network, and terminal access are available. Run each step only after its required outputs exist and validate successfully.

For example, provide the dependency tracker with:

```json
{
  "applications": [
    {"application": "continuous-clearing", "repository": "https://github.com/siemens/continuous-clearing", "branch": "main"},
    {"application": "vector", "repository": "https://siemens.ghe.com/portfolio/acdc-tc-vector", "branch": "main"}
  ]
}
```

For the compatibility analyzer, reference the release diff and successful catalog paths, for example:

```text
reports/sw360-api-diff-sw360-20.2.0-rc-1.json
catalogs/continuous-clearing-api-catalog.json
catalogs/vector-api-catalog.json
```

The release analyzer discovers release tags itself; do not hardcode a baseline or fabricate a spec. The dependency tracker scans only the requested branch and must not overwrite application source. Private repositories need suitable access. Never include credential values in generated catalogs or reports.

## GitHub Actions

The workflow in `.github/workflows/sw360-compatibility-analysis.yml` is a separate automation entry point. Before relying on it, verify its agent paths, action availability and input contract, permissions for any commit/push, cleanup order, and handling of failed scans or missing outputs against the current agent definitions. Provide the required SW360 and private-repository access, and any Docker/build dependencies needed to obtain release specs. Inspect the workflow run logs and generated artifact to confirm the full sequence succeeded.
