# Contributing to EllipticBit.SDLang

Thanks for your interest in improving EllipticBit.SDLang! This document describes how to build, test, and
contribute changes.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or newer.
- A Git client and a GitHub account.

## Building and testing

The repository uses the `SDLangNET.slnx` solution at the root.

```shell
# Restore and build the entire solution.
dotnet build SDLangNET.slnx

# Run the full test suite.
dotnet test SDLangNET.slnx

# Produce NuGet packages locally.
dotnet pack SDLangNET.slnx -c Release
```

## Coding guidelines

- Target `net10.0` and keep `nullable` reference types clean (no new warnings).
- Prefer UTF-8/`Span`-based code paths; avoid unnecessary conversions to/from `string` (UTF-16).
- Match the existing code style — tabs for indentation, file-scoped namespaces, and XML doc comments on public APIs.
- Add or update tests for any behavior change. New parsing features should include UTF-8 cases (including emoji
  and Kanji) and hardening cases for malformed input.
- Keep the build warning-free; analyzers are enabled.

## Pull requests

1. Fork the repository and create a feature branch.
2. Make your change with accompanying tests.
3. Ensure `dotnet build` and `dotnet test` succeed locally.
4. Open a pull request describing the motivation and the change.

## LLM-assisted contributions

This project welcomes contributions created with the help of Large Language Models. To keep changes auditable and
reproducible, **any contribution generated (in whole or in part) by an LLM must include the prompt(s) used to
generate it** in the [`PROMPTS.txt`](PROMPTS.txt) file at the repository root. Append your prompt(s) to that file
as part of the same pull request.

## Reporting issues

Please file bugs and feature requests through GitHub Issues. For parsing bugs, include a minimal SDLang snippet
that reproduces the problem.

## License

By contributing, you agree that your contributions will be licensed under the
[Boost Software License 1.0](LICENSE).
