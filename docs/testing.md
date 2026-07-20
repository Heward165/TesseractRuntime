# Verification strategy

TesseractRuntime uses several independent test layers because a managed unit test cannot prove
that a native OCR distribution is loadable, correctly licensed, or usable from a NuGet package.

## Deterministic managed tests

The xUnit suite covers validation, pixel ownership and stride rules, engine initialization and
failure cleanup, output selection, cancellation boundaries, pooling and lease races, ordered batch
failures, language ranking, both image adapters, native resolver policy, and the declared C ABI.
It runs on .NET 8 and .NET 10 on Windows, Linux, and macOS.

Generated interop declarations and operating-system loader calls are excluded from the managed
coverage percentage because executing them requires a real native library. The remaining managed
policy surface has hard CI gates of 96% for both line and branch coverage. The excluded boundary is
covered by native integration tests instead.

## Dependency-free conformance checks

The conformance executable repeats the most important behavioral contracts without a test
framework: owned buffers, rejected formats, immutable configuration, combined models, fixed pool
capacity, idempotent leases, stable batch ordering, and language ranking. This provides a small
second implementation of critical assertions.

## Real native OCR

Every native RID build uses pinned Tesseract 5.5.2 sources and an official Tesseract test image.
The integration program exercises:

- Gray8, RGB24, and RGBA32 buffers;
- ImageSharp and SkiaSharp conversion paths;
- plain text, hOCR, TSV, ALTO XML, and PAGE XML;
- engine reuse, bounded pooling, concurrent batch processing, and language selection;
- orientation and script detection;
- normal cleanup after all native allocations have been released.

The process exit code is checked explicitly so a late native heap failure cannot be mistaken for a
successful test after output has already been printed.

## Artifact and consumer tests

Managed packages are inspected for both target frameworks, symbols, README content, and accidental
build-file leakage. Native packages are inspected for exact RID isolation, Tesseract and Leptonica,
runtime-only files, and dependency license notices. Finally, a clean consumer project restores the
generated packages and performs real OCR without project references or a native-path override.

Native AOT publishing and execution are also gated. NuGet dependencies are audited for known
vulnerabilities, while Dependabot and CodeQL provide continuing repository checks.

## Scope

No finite suite can prove OCR correctness for every language, image, compiler, or operating-system
update. The preview release therefore makes its supported matrix and pinned inputs explicit, keeps
all test fixtures reproducible, and treats any untested RID package as unreleasable.
