# Examples

These files demonstrate portable Memory Capsule formats. They are examples of compact governed working state, not a rigid schema.

## Paste into a fresh session

1. Copy the contents of [`bootstrap-capsule.yaml`](bootstrap-capsule.yaml) or [`bootstrap-capsule.json`](bootstrap-capsule.json).
2. Paste the capsule into a fresh AI session.
3. Add this instruction:

   ```text
   Apply this Memory Capsule as governed working state. Preserve settled decisions, flag material conflicts or uncertainty, do not invent missing facts, and load only the context necessary for the active task.
   ```

4. Continue the active objective. If the session begins reopening settled decisions, contradicting itself, confusing discarded ideas with approved state, or relying excessively on old history, create a compact rollup such as [`session-rollup.yaml`](session-rollup.yaml) and bootstrap a clean session.

## Files

- `bootstrap-capsule.yaml` — readable YAML example
- `bootstrap-capsule.json` — equivalent JSON example
- `session-rollup.yaml` — compact checkpoint and recovery example
- `Watts_Protocol_Training_v1.2.json` — v1.2 training guidance
- `Watts_Protocol_Training_v1.2_minified.json` — compact v1.2 training guidance

YAML and JSON are transport formats, not the protocol itself. The protocol governs what state is captured, trusted, compressed, superseded, transferred, and restored.
