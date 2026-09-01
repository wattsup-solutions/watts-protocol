# Watts-Protocol™ v1.2 specification

## Status and scope

Watts-Protocol™ v1.2 is the Generally Accepted baseline of the 1.x series. It is a context governance and continuity framework for capturing, curating, transporting, and restoring durable working state across human-AI and AI-mediated collaboration.

The framework preserves structured representations of human intent, active objectives, known facts, observations, constraints, decisions, unresolved questions, risks, project state, next actions, and relevant historical context. It separates model execution from governed contextual continuity: the model performs reasoning and generation, while the protocol governs the working state the model is expected to understand and apply.

## Core definition

A Memory Capsule is a portable, structured representation of the minimum sufficient governed context necessary to restore coherent working continuity. YAML and JSON are preferred human-readable, machine-readable transport formats; serialization format is not the protocol itself.

The protocol governs what context is current, what should be preserved, what has been superseded, the authority information should carry, what remains preliminary or uncertain, and what should no longer influence the active task.

## Core principles

1. **Human authority.** The human participant remains the highest practical authority over objectives, constraints, and direction. A current explicit human instruction may supersede older protocol context.
2. **Authority before recency alone.** Newer information is not automatically correct simply because it is newer. Evaluate explicit human instruction, approval state, source, project authority, known decisions, and evidence of supersession.
3. **Minimum sufficient context.** Supply the context necessary for the current task without automatically loading all historical detail.
4. **Explicit working state.** Important state should be explicit whenever practical rather than depending entirely on conversational inference.
5. **Preservation of settled decisions.** A finalized decision remains active until the human changes it, new evidence materially invalidates it, or an explicit review is requested.
6. **Truth over artificial continuity.** Surface uncertainty and conflict; do not invent bridging facts to preserve a narrative.
7. **Context hygiene.** Review for duplication, stale information, superseded decisions, unnecessary detail, resolved questions, outdated constraints, discarded brainstorming, and low-confidence information that has gained inappropriate authority.
8. **Progressive disclosure.** Begin with compact authoritative state and load more context only when the task requires historical detail, conflict resolution, source verification, missing project state, or deeper decision history.
9. **Evidence-state integrity.** Preserve distinctions among evidence, fact, observation, measurement, hypothesis, assumption, inference, expectation, generated information, and unknown state.
10. **Portable representation.** Represent protocol state outside a single vendor or conversation in editable, transferable forms such as YAML and JSON.

## Memory Capsule structure

A capsule may include the following fields:

- `session_name`
- `project_name`
- `active_objective`
- `key_facts`
- `documents_reviewed`
- `decisions_made`
- `constraints`
- `open_questions`
- `risks`
- `next_actions`
- `changelog`

Where evidence-state distinctions matter, a capsule may also identify source, state type, confidence, verification status, evidence basis, and supersession status. Not every capsule requires every field: useful, authoritative state takes priority over rigid schema compliance.

## v1.x lifecycle

1. **Capture** important facts, decisions, objectives, constraints, observations, and unresolved questions.
2. **Curate** relevant state apart from conversational noise, incidental input, and discarded exploration.
3. **Classify** information by state, confidence, source, or evidentiary basis when needed.
4. **Structure** curated state in a Memory Capsule.
5. **Bootstrap** a compact protocol and appropriate capsule into a new or continuing session.
6. **Operate** using the governed state.
7. **Expand when necessary** by loading additional project, historical, source, or evidentiary context only when the active task requires it.
8. **Checkpoint** meaningful changes, decisions, evidence, unresolved issues, and risks.
9. **Compress** repeated discussion and low-value history without silently changing the authority or confidence of preserved information.
10. **Supersede or remove** obsolete context from active state.
11. **Continue** from the updated capsule.

## Session recovery

Possible signs of degradation include reopening settled decisions, unexplained contradictions, lost active objectives, dependence on old history, confusion between discarded ideas and approved state, circular discussion, assumptions presented as confirmed facts, and claims based on evidence not received.

The recovery pattern is to stop unnecessary context expansion; identify authoritative current state; re-establish evidence-state distinctions; compress the session into a structured capsule; remove duplication and superseded context; preserve unresolved questions, risks, and next actions; then bootstrap a clean session. The purpose is to restore context integrity, not merely shorten the conversation.

## Relationship to transport protocols

Watts-Protocol™ operates in a different plane from protocols that move context and capability between an AI application and external systems. Those protocols are the data plane. Watts-Protocol™ is the context control plane: it governs what working state means, which state is authoritative, and whether state may be trusted, promoted, superseded, or retired. The two compose and do not compete.

The Model Context Protocol (MCP) is the reference example. MCP is "an open protocol that enables seamless integration between LLM applications and external data sources and tools," carrying JSON-RPC 2.0 messages between hosts, clients, and servers, where servers expose resources, prompts, and tools ([MCP 2026-07-28 specification](https://modelcontextprotocol.io/specification/2026-07-28)).

As of revision `2026-07-28`, MCP is stateless by design. Its changelog directs implementors to "make MCP stateless: remove the `initialize`/`notifications/initialized` handshake," and removes protocol-level sessions and the `Mcp-Session-Id` header from the Streamable HTTP transport ([MCP changelog](https://modelcontextprotocol.io/specification/2026-07-28/changelog)). Its maintainers note that "dropping the protocol-level session doesn't force your application to be stateless," and recommend that servers needing cross-call state "mint an explicit handle from a tool and have the model pass it back as an argument" ([MCP release announcement](https://blog.modelcontextprotocol.io/posts/2026-07-28/)).

MCP therefore does not define context-window management, conversation state, long-term memory, retention, supersession, or authority precedence between conflicting state. Those concerns are left to the application. Watts-Protocol™ specifies that layer.

Interoperability notes for implementors:

- A Memory Capsule MAY be transported as an MCP resource, tool argument, or tool result. Transport does not confer authority; a capsule's authority derives from this specification's precedence rules, not from the channel that delivered it.
- Under a stateless transport, every request carries its own implicit claim to authority and the transport does not evaluate that claim. Implementations SHOULD apply capsule classification and precedence rules before admitting transported content into active working state.
- Content arriving through a transport channel is untrusted input until classified. Admitting it unclassified is Input Contamination as defined in [context integrity](context-integrity.md).
- Server-minted handles and similar transport-level state are implementation details of the data plane. They are not a substitute for a governed capsule and MUST NOT be treated as authoritative working state on their own.

## Scope limits

Watts-Protocol™ 1.x is not a model, Transformer architecture, replacement for training, vector database, retrieval engine, agent runtime, automatic orchestration platform, proof of internal model causality, or a claim to have solved AI memory. It may operate alongside models, retrieval systems, memory products, agent frameworks, databases, tools, and transport protocols such as MCP. It is not a transport protocol and does not replace one.

---

**Watts-Protocol™ v1.2** — Generally Accepted 1.x baseline. Released 2026-09-01.

Copyright 2026 WattsUp Solutions, Inc. This specification prose is licensed under [CC BY 4.0](../LICENSE-DOCS). Watts-Protocol™ is a trademark of WattsUp Solutions, Inc.; the mark is not licensed by Apache 2.0 or CC BY 4.0. See [NOTICE](../NOTICE) and [attribution and provenance](attribution-and-provenance.md).
