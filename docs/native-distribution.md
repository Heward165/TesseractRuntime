# Native distribution

Native OCR is a supply-chain problem. Every release combines Tesseract, Leptonica, image codecs,
an operating system, a CPU architecture, and a compiler ABI.

## Reproducible inputs

- Tesseract port version: `5.5.2`
- vcpkg baseline: `a51bb4d1434e6d0927ff79db8033bed8522b85df`
- language fixture: `tessdata_fast` tag `4.1.0`
- native build inputs: `native/vcpkg.json` and explicit overlay triplets

The baseline is a commit, not a moving branch. vcpkg downloads upstream source archives and checks
their hashes. The workflow retains license notices for every copied dependency.

## Matrix

| RID | GitHub runner | Linkage |
| --- | --- | --- |
| `win-x64` | Windows Server 2025 x64 | shared libraries, static C runtime |
| `win-arm64` | Windows 11 ARM64 | shared libraries, static C runtime |
| `linux-x64` | Ubuntu 24.04 x64 | shared libraries, `$ORIGIN` RPATH |
| `linux-arm64` | Ubuntu 24.04 ARM64 | shared libraries, `$ORIGIN` RPATH |
| `osx-x64` | macOS 15 Intel | shared libraries, `@loader_path` dependencies |
| `osx-arm64` | macOS 15 Apple Silicon | shared libraries, `@loader_path` dependencies |

Each matrix job builds on the target architecture, collects the runtime dependency closure,
downloads a fixed OCR fixture and language model, runs the managed integration program against the
new binaries, and only then packs `TesseractRuntime.Native.<rid>`.

## Release gate

A native package is releasable only when:

1. the C# unit and conformance suites pass;
2. the package loads on its target architecture;
3. real OCR contains the expected phrase;
4. plain text, hOCR, and TSV are non-empty;
5. Tesseract reports a compatible version;
6. dependency notices are present;
7. NuGet and CodeQL security checks are clean.

Rebuilding is required when Tesseract or any native dependency receives a security update. Merely
changing the managed package version is not sufficient.
