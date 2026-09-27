# Agent Instructions

## XML Documentation

The XML documentation ships in the package, so IntelliSense and the Cake website show it. Follow the
[Cake documentation guidelines](https://cakebuild.net/community/contributing/documentation):

- Every public type and member in `src/Cake.Grype/` needs XML docs. Missing or broken docs fail every
  build (see `src/Cake.Grype/Cake.Grype.csproj`).
- Every alias needs `<summary>`, a `<param>` for each parameter, `<returns>` when it returns a value,
  `<exception>` for what it throws, and a script-style `<example><code>`. Settings overloads need
  their own example that shows some settings.
- Every namespace needs a `NamespaceDoc` class in `src/Cake.Grype/Namespaces.cs`.
- `DocumentationTests` enforces the alias and namespace rules on the generated XML file.
- The examples are script-style, so compile-check the ones you touched as `context.Alias(...)` calls
  in a scratch project that references the built add-in. Nothing tests them automatically.

## Changelog

`CHANGELOG.md` follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and is the source of
the release notes (see `docs/release-policy.md`). Update it in the same change as the work:

- **What to add:** any change a user of the package could notice, such as new or changed aliases and
  settings, bug fixes, supported Cake or Grype versions, target frameworks, performance and package
  contents (including the README and XML docs). Leave out CI, tests, the repository's own build,
  refactoring and internal docs.
- **Where:** under `## [Unreleased]`, in `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed` or
  `Security`. Create the subsection if it isn't there, and keep them in that order.
- **How:** one bullet per change, written for the add-in's users rather than as a commit message.
  Name the alias or setting in backticks, and link the issue when there is one, such as
  `([#7](https://github.com/mgnslndh/Cake.Grype/issues/7))`. Update an existing bullet rather than
  adding a second one for the same feature.
- **Breaking changes:** start the bullet with `**Breaking:**` and say how to update. This includes
  renamed public parameters, since build scripts can pass them by name.
- **Don't** add version headings or dates. Those are added when a release is tagged.

For a stable release, also update the README install snippets (`#addin …&version=X.Y.Z` and
`#:package Cake.Grype@X.Y.Z`) to the new version; the `Release-Notes` gate enforces this.
