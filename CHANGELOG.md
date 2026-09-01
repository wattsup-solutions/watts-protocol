# Changelog

All notable changes to Watts-Protocol™ are documented in this file.

## v1.2.1 — Rules Engine Corrections

**Tagline:** "Trust what the checker tells you."

A patch release. No normative protocol behavior changed; the specification, the training
files, and the capsule schema are identical to v1.2. Every fix below corrects the
reference CLI implementation, and all were found by using the tool on real capsules
rather than by review.

**First-run experience.** The solution file is now at the repository root, so a bare
`dotnet build` succeeds on a fresh clone; previously it failed with MSB1003. Console
output is explicitly UTF-8, so the trademark symbol renders correctly on consoles whose
default code page is not UTF-8.

**Rules engine correctness.** A single well-labelled capsule previously produced 29
findings for 3 distinct problems, none of them real. Three causes were corrected:

- In `decisions_made`, `constraints`, and `next_actions`, an authority marker is the
  evidentiary basis for the item, and now satisfies the evidence-state rule on its own
  rather than being reported alongside it.
- `open_questions` is exempt from the authority-marker rule. An open question has no
  source, approval, or verification by definition, so the rule was unsatisfiable there.
- The low-confidence-state-promotion detector no longer compares item metadata as claim
  content. A structured item's text is its serialised form, so every labelled item
  shared the field names `text`, `state_type`, `confidence`, and `source` with every
  other one and cleared the overlap threshold on metadata alone. Detection now reads the
  claim's prose only, distinguishes strong assertions from bare copulas with separate
  overlap thresholds, exempts restatements that carry recorded validation, ignores scalar
  header fields, and reports the finding against the hedged item rather than the
  restatement.

The stale-or-expired and superseded-item rules likewise required a passing mention of
those terms in prose to be treated as a marking. Both now require an explicit marking
construction or a dated expiry, so a capsule that discusses replacing outdated material
is no longer reported as outdated itself.

**Reporting.** `wp check` groups findings by rule, with item counts and affected
sections, and `--verbose` restores per-item rows. JSON output is unchanged, so existing
automation continues to work.

**Schema note.** The `constraints` field was introduced in the v1.2 capsule schema and is
documented in the specification and both training files. Capsule templates predating v1.2
do not include it; this is expected and requires no change to the field set.

The bundled example capsule now passes the tool's own `wp check` with no findings. Test
coverage rose from 12 to 35 cases, including the capsule that reproduced the reporting
failure, retained as a fixture.

## v1.2 — Operational Compression and Context Portability

**Tagline:** “Load only what you need.”

Introduced short-first loading, selective context expansion, progressive compression, smaller Memory Capsules, JSON portability, stronger separation of protocol and project context, deliberate session recovery, and terminology for context-integrity conditions. This is the operational maturity of the established 1.x series.

**Editorial addition, 2026-08-28:** added a non-normative "Relationship to transport protocols" section to the specification, positioning Watts-Protocol™ as the context control plane above data-plane transport standards such as MCP, with interoperability notes for implementors. No normative v1.2 behavior changed.

**Release-date note:** v1.2 was declared Generally Accepted internally on 2026-08-21 and was frozen from that date forward. The public release date recorded in `CITATION.cff`, the specification footer, and the version table is **2026-09-01**, the date of first public availability and of the Zenodo deposit (DOI 10.5281/zenodo.22224272). Entries in this changelog that carry earlier dates — such as the 2026-08-28 editorial addition — record when the work was done, not when it was published. Both dates are accurate; they answer different questions.

## v1.1 — Governed Contextual Continuity

**Tagline:** “Know what to trust.”

Established context authority, instruction precedence, human override, conflict handling, context hygiene, preservation of settled decisions, explicit uncertainty, project-scoped context, controlled context inheritance, and cascading parent and child context.

## v1.0 — Session Bootstrap and Portable Working Memory

**Tagline:** “Remember this.”

Introduced structured Memory Capsules, portable YAML-based working context, active objectives, project state, prior decisions, known constraints, unresolved questions, next actions, and AI bootstrap instructions for continuity across sessions.
