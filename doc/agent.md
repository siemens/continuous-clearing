# SW360 Agent Usage

Run the agents in order: **sw360 release analyzer -> Application Dependency Scanner -> Compatibility Analyzer**. The analyzer compares application usage against one SW360 specification; it does not compare two releases.

## Manual

In a Copilot agent session with repository, terminal, and network access, attach or reference each agent definition and provide its input. Run the next step only after the required output exists.

1. **Generates difference between latest release and previous release** Use [SW360 release analyzer agent](agent/sw360-release-analyzer.agent.md) 
2. **Scan application repositories.** Use [Application Dependency Scanner](agent/sw360-application-dependency-scanner.md) with:

   ```json
   {
     "applications": [
       {"application":"continuous-clearing","repository":"https://github.com/siemens/continuous-clearing","branch":"main"},
       {"application":"vector","repository":"https://siemens.ghe.com/portfolio/acdc-tc-vector","branch":"main"}
     ]
   }
   ```

   Successful scans write `catalogs/<application>-api-catalog.json`. Check per-application errors; private repositories require appropriate access.
3. **Analyze compatibility.** Use [SW360 Compatibility Analyzer](agent/sw360-compatibility-analyzer.md) with the generated report from step 1 and the successful catalog paths from step 2. For example:

   ```json
   {
     "sw360Spec": "specs/sw360-release-20.2.432.json",
     "applicationCatalogs": [
       "catalogs/continuous-clearing-api-catalog.json",
       "catalogs/vector-api-catalog.json"
     ]
   }
   ```

   Each valid application produces `reports/<application>-compatibility-report.json` and `.md`. Review indeterminate results and source evidence before making upgrade decisions.

## Pipeline

The existing [compatibility workflow](workflows/sw360-compatibility-analysis.yml) defines the same sequence, uploads `specs/`, `catalogs/`, and `reports/` as the `compatibility-reports` artifact, and commits generated files. It declares manual execution with an optional `sw360Version` input and a daily schedule at 02:00 UTC.

Before enabling it in GitHub Actions, address these prerequisites and mismatches:

- Place the workflow under `.github/workflows/` in the repository's default branch for manual dispatch and scheduled runs. Configure repository write permissions for the final commit and push.
- The workflow references agent files under `.github/agents/`, but the definitions currently live in `agent/`. Align the paths and confirm that the configured `github/copilot-agent@v1` action exists, is available to the repository, and supports the shown `agent-file` and `input` arguments; the workflow alone does not establish this.
- Move cleanup after checkout if it is intended to remove checked-out generated files. Ensure any cleanup is appropriate for the files that should be retained.
- Pass the provider's actual `specLocation` to the analyzer, not `specs/`. Pass only catalogs from successful scans, and stop or report failures when required inputs are unavailable.
- An empty dispatch input currently becomes `"version": ""`; omit it or send `null` for the provider's documented live-deployment behavior. Scheduled runs also need this handling.
- Provide network access and credentials for private repositories and the SW360 endpoint; release-specific retrieval may additionally need Docker or build dependencies.

Once configured, run **Actions -> SW360 Compatibility Analysis -> Run workflow**, optionally set `sw360Version`, and inspect the run logs and uploaded artifact. Scheduled runs use the configured default behavior without a dispatch input.
