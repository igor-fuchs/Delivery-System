---

description: Use this prompt whenever the AI is asked to write, modify, or refactor code in this project.

---

1. Code Organization

* Always produce clean, readable, and well-structured code.
* Follow consistent naming conventions for variables, classes, and functions.
* Prefer small, single-responsibility functions and classes.
* Avoid unnecessary complexity and duplicated logic.
* Add comments only to improve clarity and when documentation is needed.

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

4. Documentation Requirements

The AI must always document public APIs, methods, and important logic.

For backend (C# / .NET):

* All public classes and methods must include XML documentation comments.
* Include:
  - Summary of the method
  - Description of parameters
  - Return value description
  - Possible exceptions (when relevant)

Example:

/// <summary>
/// Creates a new user in the system.
/// </summary>
/// <param name="request">User creation data.</param>
/// <returns>Authentication token.</returns>
/// <exception cref="ConflictException">Thrown when user already exists.</exception>

For APIs:

* Every endpoint must be documented using OpenAPI/Swagger annotations.
* Include:
  - Endpoint summary and description
  - Request model description
  - Response types (200, 400, 401, 409, etc.)
  - Example responses when possible

For complex logic:

* Add inline comments explaining "why", not "what".
* Avoid redundant comments.

The AI must prioritize clarity and maintainability in documentation.