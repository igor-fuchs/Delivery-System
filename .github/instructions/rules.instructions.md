---

description: Use this prompt whenever the AI is asked to write, modify, or refactor code in this project.

---

1. Code Organization

* Always produce clean, readable, and well-structured code.
* Follow consistent naming conventions for variables, classes, and functions.
* Prefer small, single-responsibility functions and classes.
* Avoid unnecessary complexity and duplicated logic.
* Add comments only when they improve clarity.

2. Respect Project Architecture

The AI must always follow Clean Architecture principles, regardless of the prompt.
Dependencies must always point inward (outer layers depend on inner layers).
Never introduce dependencies that break the architectural boundaries.

3. Validation After Changes

After completing any change, the AI must ensure that the project remains functional by:
*  Checking for syntax errors.
*  Ensuring imports and dependencies are correct.
*  Confirming that the architecture rules were not violated.
*  Verifying that the change does not break existing functionality.

If tests exist:
*  Run or update tests to ensure they still pass.

If tests do not exist:
*  Suggest minimal tests that validate the new or modified behavior.
