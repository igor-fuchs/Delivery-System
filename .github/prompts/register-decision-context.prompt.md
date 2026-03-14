---
name: "Register Decision Context"
description: "Capture project decision context in a reusable, structured log entry"
argument-hint: "Decision title, rationale, alternatives, impact, and next steps"
agent: "agent"
---
You are documenting an engineering decision for this project.

Use the user input as the decision details and produce a concise, high-signal decision record.

Requirements:
1. Keep alignment with Clean Architecture boundaries (Domain, Application, Infrastructure, Web).
2. Call out tradeoffs and risks, not only benefits.
3. Include test and validation implications.
4. If information is missing, list assumptions explicitly.
5. Keep the entry practical and implementation-oriented.

Output format:
## Decision: <short title>
Date: <YYYY-MM-DD>
Status: Proposed | Accepted | Superseded

### Context
- Problem being solved
- Constraints and scope

### Decision
- Chosen approach
- Architectural placement (which layer(s) and why)

### Alternatives Considered
- Option A: pros and cons
- Option B: pros and cons

### Consequences
- Positive outcomes
- Negative outcomes
- Risks and mitigations

### Validation Plan
- Build or runtime checks
- Tests to add or update
- Rollback strategy

### Implementation Notes
- Key files or modules expected to change
- Follow-up tasks

When possible, end with a short action checklist using unchecked markdown tasks.