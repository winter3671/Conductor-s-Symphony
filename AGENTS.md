# Project Guidelines & Agent Instructions

This project incorporates the **Superpowers** framework (`obra/superpowers`) for software development discipline.

## Core Rules for AI Agents (Antigravity / Claude Code / Cursor)

1. **Superpowers Methodology**:
   - Always check and invoke available skills located in `.gemini/skills/` (e.g. `using-superpowers`, `brainstorming`, `writing-plans`, `test-driven-development`, `systematic-debugging`, `verification-before-completion`).
   - Prioritize structured thinking, plan creation, and testing before jumping directly to implementation.

2. **Workflow Order**:
   - **Brainstorming & Planning**: Refine requirements and outline implementation steps before coding.
   - **Test-Driven Development (TDD)**: Write tests first where applicable.
   - **Systematic Debugging**: Perform careful root-cause analysis when resolving bugs.
   - **Verification**: Always verify clean build, tests, or empirical runtime success before declaring tasks complete.

3. **Team Roles & Workflow Rules**:
   - **Role 1 (Game Designer / Audio Lead)**:
     - Balance tuning, 32-beat note patterns (`InstrumentPatternDatabase.cs`), enemy spawn scaling, weapon stats.
   - **Role 2 (UI / UX & Pixel Artist)**:
     - 2D pixel art sprites (`Assets/Resources/Sprites/`), UI layout designs & UI prefabs (`Assets/Prefabs/UI/`).
   - **Role 3 (Main Gameplay Programmer)**:
     - Core C# gameplay logic (`Assets/Scripts/`), rhythm engine, enemy/boss AI, combat mechanics, event-driven architecture.
   - **Strict Prefab Rule (Git Multi-Developer Protection)**:
     - **NEVER modify `Gameplay.unity` directly** during feature implementation!
     - Always create, edit, and bind components inside Prefabs (`Assets/Prefabs/`) using Prefab Mode to prevent Git YAML merge conflicts with team members.
   - **AI Agent Context Scoping**:
     - The AI agent MUST align its work based on the active Git branch (`feat/gameplay_core`, `feat/ui_artist`, etc.) and the developer's explicit prompt.
