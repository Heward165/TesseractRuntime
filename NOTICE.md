# Third-party notices

TesseractRuntime managed code is independently developed and MIT licensed.

Native runtime packages are built from Tesseract 5.5.2 (Apache-2.0), Leptonica
(BSD-2-Clause), and their codec/runtime dependencies. Each generated package contains the exact
copyright files collected from its pinned vcpkg installation under `licenses/`.

`TesseractRuntime.ImageSharp` references SixLabors.ImageSharp under the Six Labors Split License.
`TesseractRuntime.SkiaSharp` references SkiaSharp under the MIT License. These dependencies are not
vendored into the managed source repository.
