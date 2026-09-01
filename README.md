# Watts-Protocol™ v1.2

**Watts-Protocol™ is a portable governance layer for the context an AI is expected to trust and apply.** It keeps durable human intent, decisions, constraints, evidence state, and next actions explicit when collaboration crosses sessions, tools, agents, and AI systems. v1.2 makes continuity operationally disciplined: preserve the state that matters, remove what no longer does, and load only what the task needs.

Watts-Protocol™ is a framework for governed contextual continuity across human-AI and AI-mediated collaboration. It externalizes important working state into compact, human-reviewable **Memory Capsules** that can be loaded, reviewed, updated, compressed, transferred, and reused. The protocol governs the working state an AI is expected to understand and apply; models remain responsible for reasoning and generation.

**Sites:** [WattsUp Solutions](https://wattsupsolutions.com) · [Protocol site](https://protocol.wattsupsolutions.com)

## The problem

Long-running AI collaboration accumulates obsolete instructions, discarded ideas, duplicated facts, unresolved contradictions, stale assumptions, preliminary observations, finalized decisions, and changed priorities. Availability is not the same as authority, relevance, confidence, or evidence. Large context windows can reduce some information loss, but they do not independently establish context governance.

## Context-integrity failure modes

v1.2 names the working conditions that can make a collaboration unreliable:

- **Evidence-Grounded State (EGS):** the desired state in which verified facts, observations, hypotheses, assumptions, inferences, expectations, generated information, and unknowns remain clearly distinguished.
- **EGS-Drift:** loss of those evidence-state distinctions.
- **Input Contamination:** unintended, incidental, ambient, or otherwise non-authoritative input begins influencing active working context.
- **Low-Confidence State Promotion:** low-confidence information gains authority without sufficient validation, evidence, or confirmation.
- **False Perceptual Attribution:** an AI claims or implies it saw, heard, received, measured, or otherwise observed evidence it did not possess.
- **Context Integrity Degradation:** the broader condition in which context loses important distinctions, authority relationships, or continuity state.

The core principle is: **Do not promote contextual state beyond the authority or confidence supported by its evidentiary basis.**

## What v1.x is—and is not

Watts-Protocol™ 1.x is an established framework for capturing, curating, transporting, and restoring durable working state. It provides a practical lifecycle: capture, curate, classify when needed, structure in a Memory Capsule, bootstrap, operate, expand only when necessary, checkpoint, compress, supersede or remove, and continue.

It is **not** a large language model, Transformer architecture, replacement for model training, vector database, retrieval engine, general agent runtime, automatic orchestration platform, proof of an internal model mechanism, or a claim to have permanently solved AI memory. It operates alongside models, retrieval systems, memory products, agent frameworks, databases, and tools by governing how important context is selected, structured, classified, trusted, compressed, transferred, superseded, and restored.

## Quickstart: bootstrap a fresh session

1. Copy the capsule below or open [`examples/bootstrap-capsule.yaml`](examples/bootstrap-capsule.yaml).
2. Paste it into a fresh AI session, followed by: “Apply this Memory Capsule as governed working state. Preserve settled decisions, flag material conflicts or uncertainty, do not invent missing facts, and load only the context necessary for the active task.”
3. Continue work. At a meaningful checkpoint, update the capsule or create a compact rollup using [`examples/session-rollup.yaml`](examples/session-rollup.yaml).

```yaml
session_name: watts-protocol-v1.2-bootstrap-example
project_name: Watts-Protocol™
active_objective: Continue the active task with minimum sufficient governed context.
key_facts:
  - value: Watts-Protocol™ v1.2 is the Generally Accepted 1.x baseline.
    state_type: verified_fact
    verification_status: established
  - value: v2.x is exploratory and non-normative.
    state_type: verified_fact
    verification_status: established
decisions_made:
  - Use current explicit human instruction over stale historical context.
constraints:
  - Preserve distinctions among evidence, observation, hypothesis, assumption, inference, expectation, generated information, and unknown state.
  - Do not invent missing facts.
open_questions: []
risks:
  - Context bloat or low-confidence state promotion can degrade context integrity.
next_actions:
  - Perform the active task using this capsule.
changelog:
  - v1.2: load only what is needed; expand context only when the task requires it.
```

## Repository guide

- [`docs/specification.md`](docs/specification.md) — v1.2 framework, core principles, Memory Capsules, and lifecycle
- [`docs/context-integrity.md`](docs/context-integrity.md) — EGS and the named failure conditions
- [`docs/governance.md`](docs/governance.md) — authority, precedence, settled decisions, and human control
- [`docs/roadmap.md`](docs/roadmap.md) — clearly non-normative v2.x exploration
- [`examples/`](examples/) — copy-paste capsule and training-file examples

## Licenses and contribution back

Code, examples, capsules, and CLI material are available under [Apache License 2.0](LICENSE). Specification prose and [`docs/`](docs/) are available under [CC BY 4.0](LICENSE-DOCS). Contributions are welcome but never required; use, implementation, adaptation, and adoption do not require contribution back. The Watts-Protocol™ name and mark are not licensed by Apache 2.0; see [NOTICE](NOTICE).

## Citation

> Watts, Douglas. (2026). *Watts-Protocol™ v1.2*. WattsUp Solutions, Inc. https://doi.org/10.5281/zenodo.22224272

See [`CITATION.cff`](CITATION.cff) for machine-readable citation metadata.
