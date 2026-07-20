# Contributing

Changes should preserve the separation between the stable C ABI, managed ownership, concurrency
policy, image adapters, and native packaging.

Before submitting a change, run:

```bash
dotnet format TesseractRuntime.slnx --verify-no-changes
dotnet build TesseractRuntime.slnx -c Release
dotnet test tests/TesseractRuntime.Tests/TesseractRuntime.Tests.csproj -c Release
dotnet run --project tests/TesseractRuntime.Conformance -c Release
```

Native changes must also pass the affected RID jobs in the native runtime matrix. Do not update a
native version without updating its pinned vcpkg baseline, notices, integration evidence, and
changelog.
