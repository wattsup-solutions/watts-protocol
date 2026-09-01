# Context integrity and Evidence-Grounded State

## Evidence-Grounded State (EGS)

**Evidence-Grounded State (EGS)** is the desired working state in which received evidence, verified facts, observations, hypotheses, assumptions, inferences, expectations, generated information, and unknowns remain clearly distinguished. Information does not receive greater authority or certainty than its evidentiary basis supports.

EGS does not require every statement to be proven. A healthy working state may include hypotheses, assumptions, preliminary observations, incomplete information, unresolved questions, and informed inference, provided those states remain distinguishable. For example, “The two components appear visually similar” is a preliminary observation, not automatically the same as “The two components are the same size.”

## Failure conditions

### EGS-Drift

**EGS-Drift** is movement away from Evidence-Grounded State through loss of reliable distinctions among evidence, observations, measurements, hypotheses, assumptions, expectations, inferences, generated information, and unknown state. A preliminary observation can gradually become a working assumption and later be recalled as fact.

### Input Contamination

**Input Contamination** is unintended, incidental, ambient, or otherwise non-authoritative input entering active context and influencing the collaboration. It may include incidental speech in voice interaction, self-directed speech interpreted as instruction, background conversation, irrelevant transcript material, poorly scoped retrieval results, unrelated logs, system output, or messages outside the intended task scope. The issue is not necessarily malicious input; it is failure to distinguish intentional working context from incidental or low-authority input.

### Low-Confidence State Promotion

**Low-Confidence State Promotion** occurs when a preliminary observation, tentative hypothesis, speculation, ambiguous statement, incomplete measurement, incidental self-talk, inferred intent, or assumption gains authority without sufficient validation, evidence, or confirmation. Promotion itself is not always a failure: new evidence may properly support a conclusion, measurement may confirm an observation, or a human may approve an assumption. The failure is an increase in authority or certainty without a sufficient change in the supporting basis.

### False Perceptual Attribution

**False Perceptual Attribution** occurs when an AI claims or implies that it saw, heard, received, measured, or otherwise observed evidence it did not possess. Examples include asserting that it can see a difference without an image, heard a noise without relevant audio, or that a document or measurement confirms something without access to that evidence. Such language can improperly increase confidence in a conclusion.

### Context Integrity Degradation

**Context Integrity Degradation** is the broader condition in which active working context becomes less reliable because it loses important distinctions, authority relationships, or continuity state. Context bloat, input contamination, Low-Confidence State Promotion, EGS-Drift, stale-state reuse, loss of settled decisions, contradictory context, excessive compression, and poorly scoped loading can contribute.

## Bench-measurement failure chain

A physical measurement task illustrates how several conditions can interact. During a voice-enabled AI session, a user visually compares two small physical components before taking precise measurements. The user says aloud, “These already look almost the same size,” and, “Maybe the first one was never ground down anyway.” These are a preliminary observation and speculation based on unaided visual inspection.

Voice input remains active, and speech enters the conversation even though some of it may have been self-directed rather than formal instruction. At this point, the visual observation is low confidence, precise measurement has not occurred, an expected photograph has not been supplied, and the user has not confirmed a conclusion.

A possible failure chain is:

```text
Evidence-Grounded State
          ↓
Input Contamination
          ↓
Low-Confidence State Promotion
          ↓
EGS-Drift
          ↓
False Perceptual Attribution
```

The AI might treat the preliminary observation as established state and respond as though expected visual evidence had been received: “I see what you mean. They are already nearly the same.” If no image was received, it has moved beyond an unsupported inference and implied possession of visual evidence. Later measurement may show that the components differ. The important failure is not only an incorrect conclusion: a low-confidence observation changed state without retaining its confidence and evidence classification.

This distinction separates source traceability from evidence-state integrity. A system may know where information came from while still losing track of what kind of information it was when received.
