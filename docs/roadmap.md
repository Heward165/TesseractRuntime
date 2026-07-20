# Roadmap

## Preview stabilization

- run the six-RID native matrix on every candidate release;
- add malformed-image and native fault-injection tests;
- record per-RID binary sizes, startup time, throughput, and peak memory;
- validate trimming and Native AOT sample applications;
- add signed provenance and a software bill of materials for native packages.

## Recognition APIs

- safe word/line/symbol iterators with bounding boxes;
- searchable PDF renderer integration without hiding native ownership;
- streaming file queues with explicit memory budgets;
- configurable retry policy for transient native failures;
- benchmark corpora for Latin, CJK, RTL, and mixed-script documents.

## Detection

- calibrated language scoring beyond mean confidence;
- optional script-first candidate reduction;
- OSD quality diagnostics for sparse pages;
- rotation pipeline adapters that preserve original coordinates.

Language detection will remain opt-in and measurable. The project will not market repeated OCR
passes as a cost-free automatic feature.
