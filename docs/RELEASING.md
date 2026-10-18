# Release checklist

1. Update Version.props and release notes. Keep the installer AppId, old IPC prefix, and settings version compatible unless a tested migration is included.
2. Run the locked build, formatting checks, application regressions, and `tools/verify.ps1 -Interactive` on Windows 11. Resolve failures; record unavailable checks honestly in VALIDATION.md.
3. Run the benchmark and a real 30-minute maximum-load soak. Record frame timing, CPU, private memory, GDI/USER handles and GC counts. A useful local target is p95 rendering below 33.3 ms at 1280×752; shared CI timing is informational.
4. Commit the intended source. Package, verify the real 0.3.1 upgrade/fresh install/uninstall, and verify extracted portable/source ZIPs with spaces in their paths. Publish no working data or logs.
5. Wait for Windows validation on the exact release commit. Verify remaining interactive limits and confirm the release notes match the implementation.
6. Create the matching version tag. Run the Publish beta workflow for that existing tag, or publish the three locally verified packages with GitHub CLI. Use a prerelease and include SHA-256 checksums.
7. Download all three published assets and compare their hashes. Only then replace the previous three local files in artifacts.

Example benchmark invocation after building:

```powershell
./tests/bin/Release/Goose.Tests.exe --benchmark "C:\Temp\goose-performance.txt" 30
```

Reports and staging belong in ignored .work folders. The installer verification uses a separate product identity and removes its own shortcuts. It does not provide a disposable VM or actually redirect Windows' Documents known folder.
