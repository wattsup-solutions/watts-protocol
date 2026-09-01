# Attribution and provenance

Copyright 2026 WattsUp Solutions, Inc. All rights in the Watts-Protocol™ mark reserved.

## Attribution

The repository’s Apache 2.0 license applies to code, examples, capsules, and CLI material. The CC BY 4.0 license applies to specification prose and `docs/`. Forks and derivative works must retain applicable copyright, trademark, and attribution notices, including [NOTICE](../NOTICE), as required by their governing license terms.

Watts-Protocol™ is a trademark of WattsUp Solutions, Inc. The Apache 2.0 license does not grant a trademark license except for reasonable and customary use in describing the origin of the work and reproducing the NOTICE file.

## Attribution history and version dates

| Version | Focus | Status | Date |
| --- | --- | --- | --- |
| 1.0 | Continuity — "Remember this." | Superseded | May 2026 |
| 1.1 | Authority and trust — "Know what to trust." | Superseded | 2026 |
| 1.2 | Efficiency and operational discipline — "Load only what you need." | Generally Accepted 1.x baseline | 2026-09-01 |
| 2.x | Reference architecture exploration | Exploratory, non-normative | Not released |

Authorship of the Watts-Protocol™ is attributed to Douglas Watts. Attribution was
established May 2026, as recorded in [NOTICE](../NOTICE). Rights are held by
WattsUp Solutions, Inc.

A citable DOI for the v1.2.0 release is minted through Zenodo at publication time; see
[CITATION.cff](../CITATION.cff) and the citation block in the [README](../README.md).

## Document hashes

SHA-256 hashes of the v1.2.0 release documents as published. These let any third party
verify that a copy of the specification or training file is unmodified. Recompute with
`sha256sum <file>` (or `shasum -a 256 <file>` on macOS).

| File | SHA-256 |
| --- | --- |
| `NOTICE` | `a4cb1f7e34223c2163b035f953a6b8229177f6c0e6f8ba90efb84a4f53a3d5b8` |
| `LICENSE-DOCS` | `82bc72c3e295182f69002f18a84e1dd5024358cf9415be7440b607c4d36e519d` |
| `CITATION.cff` | `0b8b2ac632d5e32d1ab58d83fa1b6dfed58eeeb5e8027ba3a8b68ea3d95ff9d7` |
| `CHANGELOG.md` | `627b012b2a3e514721878385af8d6fbd0d8786591e830b3dfbc16da6f915efd5` |
| `README.md` | `cbb22a7ab3f70ac428382a155397d2856f0b6152f800c60649d4e3bd5449a23a` |
| `docs/specification.md` | `803721c2fb86344a1e9977c48ecfc469f8b669238915a1d8ec00f4179ef6ec43` |
| `docs/context-integrity.md` | `f60e79f3cf4f4831202dff8450a0b42330b8e227a11ecb6a4cbf5c5b8cefc580` |
| `docs/governance.md` | `702951dca5bca2277d2ad55e32c6a7e01e222e8883c17514bf9cc13d01df63e4` |
| `docs/prime-clause.md` | `469e03350626f58e50a016ef9d4ddeb9a141aba69360bcf232e9285e890e2eb7` |
| `docs/roadmap.md` | `9a30c70f68f782b807685a02530e0bbf8bdacedccd746b2cea2d6334f7f2c107` |
| `docs/faq.md` | `d37f7b6b0252890fb7a17198837b3644ec3d3083320fdcf9108d4ccf7108e828` |
| `examples/Watts_Protocol_Training_v1.2.json` | `cebf7ab85d2318111f601a0b8fc4bd3c409431e4eb0533c54f82575267e97a14` |
| `examples/Watts_Protocol_Training_v1.2_minified.json` | `2a509339ba6633daf4b8154f609c89e0772bf5696cc9e60248d5a056997ce980` |
| `examples/bootstrap-capsule.json` | `f795d701c51d14960939169bf69d1e69d45cdedb6fd99eaf209b2e0cb0b9a226` |

This file is excluded from its own table. Hashes below are for the published v1.2.0
release, including the minted Zenodo DOI (10.5281/zenodo.22224272).

## Provenance and state

In v1.2, a Memory Capsule may identify a source, state type, confidence, verification status, evidence basis, and supersession status where those distinctions materially affect the task. This makes the evidentiary meaning of contextual information explicit rather than treating all remembered material as equally authoritative.

Source traceability and evidence-state integrity are different concerns. Knowing where information originated does not, by itself, preserve whether it was received as evidence, observation, hypothesis, assumption, inference, expectation, generated information, or unknown state.

## Exploratory boundary

**Contextual Chain of Custody** is a v2.x exploratory concept, not a v1.x requirement. It describes a possible traceable record of origin, movement, transformation, authority, confidence, and state changes for important context. It must not be presented as formal evidentiary chain-of-custody compliance, which may require legal, regulatory, technical, validation, and operational controls beyond the current specification.
