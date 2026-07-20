# Security policy

Security reports are accepted through GitHub private vulnerability reporting for this repository.
Do not disclose a suspected vulnerability in a public issue before a fix is available.

The maintained preview line is `0.1.x`. Reports should include the managed package version, RID,
native Tesseract version, operating system, architecture, reproduction, and impact.

Native dependency reports are in scope even when the defect originates in Tesseract, Leptonica,
or a bundled codec. A fixed upstream release still requires this project to rebuild, retest, and
redistribute every affected RID package.
