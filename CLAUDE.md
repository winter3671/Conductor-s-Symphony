# Project Guidelines & Agent Instructions

This project incorporates the **Superpowers** framework (`obra/superpowers`) for software development discipline.

## Core Rules for AI Agents

1. **Superpowers Methodology**:
   - Always check and invoke available skills located in `.gemini/skills/` (e.g. `using-superpowers`, `brainstorming`, `writing-plans`, `test-driven-development`, `systematic-debugging`, `verification-before-completion`).
   - Prioritize structured thinking, plan creation, and testing before jumping directly to implementation.

2. **Workflow Order**:
   - **Brainstorming & Planning**: Refine requirements and outline implementation steps before coding.
   - **Test-Driven Development (TDD)**: Write tests first where applicable.
   - **Systematic Debugging**: Perform careful root-cause analysis when resolving bugs.
   - **Verification**: Always verify clean build, tests, or empirical runtime success before declaring tasks complete.

3. **Git Repository Access**:
   - Do NOT run `git add`, `git commit`, `git push`, or any other command that stages or modifies repository history/state. The user handles all git operations themselves.
   - Read-only git commands (`git status`, `git diff`, `git log`, etc.) are fine for investigation.
   - If a task requires staging or committing, make the file changes and tell the user what to stage/commit — do not run it yourself.
