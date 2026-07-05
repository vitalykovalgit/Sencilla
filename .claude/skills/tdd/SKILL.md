---
name: tdd
description: Test-driven development with red-green-refactor loop. Use when user wants to build features or fix bugs using TDD, mentions "red-green-refactor", wants integration tests, or asks for test-first development.
---

# Test-Driven Development

## Philosophy

**Core principle**: Tests should verify behavior through public interfaces, not implementation details. Code can change entirely; tests shouldn't.

**Good tests** are integration-style: they exercise real code paths through public APIs. They describe _what_ the system does, not _how_ it does it. A good test reads like a specification - "user can checkout with valid cart" tells you exactly what capability exists. These tests survive refactors because they don't care about internal structure.

**Bad tests** are coupled to implementation. They mock internal collaborators, test private methods, or verify through external means (like querying a database directly instead of using the interface). The warning sign: your test breaks when you refactor, but behavior hasn't changed. If you rename an internal function and tests fail, those tests were testing implementation, not behavior.

See [tests.md](tests.md) for examples and [mocking.md](mocking.md) for mocking guidelines.

## Anti-Pattern: Horizontal Slices

**DO NOT write all tests first, then all implementation.** This is "horizontal slicing" - treating RED as "write all tests" and GREEN as "write all code."

This produces **crap tests**:

- Tests written in bulk test _imagined_ behavior, not _actual_ behavior
- You end up testing the _shape_ of things (data structures, function signatures) rather than user-facing behavior
- Tests become insensitive to real changes - they pass when behavior breaks, fail when behavior is fine
- You outrun your headlights, committing to test structure before understanding the implementation

**Correct approach**: Vertical slices via tracer bullets. One test → one implementation → repeat. Each test responds to what you learned from the previous cycle. Because you just wrote the code, you know exactly what behavior matters and how to verify it.

```
WRONG (horizontal):
  RED:   test1, test2, test3, test4, test5
  GREEN: impl1, impl2, impl3, impl4, impl5

RIGHT (vertical):
  RED→GREEN: test1→impl1
  RED→GREEN: test2→impl2
  RED→GREEN: test3→impl3
  ...
```

## Workflow

### 1. Planning

Before writing any code:

- [ ] Confirm with user what interface changes are needed
- [ ] Confirm with user which behaviors to test (prioritize)
- [ ] Identify opportunities for [deep modules](deep-modules.md) (small interface, deep implementation)
- [ ] Design interfaces for [testability](interface-design.md)
- [ ] List the behaviors to test (not implementation steps)
- [ ] Get user approval on the plan

Ask: "What should the public interface look like? Which behaviors are most important to test?"

**You can't test everything.** Confirm with the user exactly which behaviors matter most. Focus testing effort on critical paths and complex logic, not every possible edge case.

### 1.1. Edge/corner cases 

**ALWAY** write tests for edge cases. These are where bugs hide. Don't wait until the end to write them - write them as soon as you have a working implementation, before refactoring.

**ALWAYS** investigate the code to find edge cases and corner cases you might not have thought of. Look for:
- Null/undefined inputs
- Empty collections
- Maximum/minimum values
- Invalid formats
- Concurrent modifications
- Error handling paths
- And more...

### 1.2. Angular tests

 - For Angular components and services, write tests that use the real Angular testing environment. Don't mock dependencies unless they are external services (e.g., HTTP calls). Use `TestBed` to create components and inject services, and test through their public APIs. For components, use `fixture.detectChanges()` to trigger change detection and test rendered output. For services, call methods directly and verify results.

- Test components as black boxes: interact with them as a user would (e.g., click buttons, input text) and verify the resulting behavior (e.g., DOM changes, emitted events). Don't test private methods or internal state directly.

- Test for all possible combination of inputs and states, including edge cases. For example, if a component has an input that can be null, test the behavior when it is null, undefined, empty string, etc.

### 2. Tracer Bullet

Write ONE test that confirms ONE thing about the system:

```
RED:   Write test for first behavior → test fails
GREEN: Write minimal code to pass → test passes
```

This is your tracer bullet - proves the path works end-to-end.

### 3. Incremental Loop

For each remaining behavior:

```
RED:   Write next test → fails
GREEN: Minimal code to pass → passes
```

Rules:

- One test at a time
- Only enough code to pass current test
- Don't anticipate future tests
- Keep tests focused on observable behavior

### 4. Refactor

After all tests pass, look for [refactor candidates](refactoring.md):

- [ ] Extract duplication
- [ ] Deepen modules (move complexity behind simple interfaces)
- [ ] Apply SOLID principles where natural
- [ ] Consider what new code reveals about existing code
- [ ] Run tests after each refactor step

**Never refactor while RED.** Get to GREEN first.

## Traps That Pass Unit Tests

### Test the full wiring, not just each piece
Each component can be correct in isolation yet break when composed. A common pattern: Component A writes a value programmatically, but the parent treats every write as user-initiated and triggers a side effect. Unit tests pass for A and for the parent separately — the bug only appears when both are wired together.

**Rule**: When multiple components share a value channel (one writes, another reacts), write at least one test that exercises the **full composition** — all participants wired together, verifying that programmatic writes don't trigger handlers meant for user actions.

### Name booleans after their binding, not their concept
A test asserting `isVisible() === false` verifies the boolean is correct, but says nothing about **how the consumer uses it**. Hiding an element (removing from DOM) and disabling it (keeping it rendered but inert) are very different behaviors behind the same `false` value. If the boolean is named after the abstract concept ("visible") rather than the consumer's action ("disabled"), the mismatch between intent and implementation is invisible in tests and code review alike.

**Rule**: Name boolean properties/signals after **what happens when they're true** — `isDisabled`, `shouldCollapse`, `isReadonly` — not after abstract states like `isVisible` or `isActive`. When the name matches the binding (`[disabled]="isDisabled()"`), a mismatch between intent and template becomes self-evident.

## Checklist Per Cycle

```
[ ] Test describes behavior, not implementation
[ ] Test uses public interface only
[ ] Test would survive internal refactor
[ ] Code is minimal for this test
[ ] No speculative features added
```
