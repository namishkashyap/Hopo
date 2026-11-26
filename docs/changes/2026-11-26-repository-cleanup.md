# Repository and folder cleanup

Owner's request: remove files that are not needed and reduce the size, then push to GitHub.

- [x] Remove from the repository: previews/ (67 screenshots of the 2.1.1 design, 16 MB, no longer used by the README), START HERE.txt and SHA256SUMS.txt (Lantoss 1.9 instructions and checksums), package-report.json and payload-verification.json (1.9.0 build reports). Their history stays in git.
- [x] Local folder (not in git): delete rebuildable build output and old test output in work\ (artifacts, artifacts-admin, artifacts-android, pack, pack-artifacts, installer-objects-*, release-2.6.3, release-2.6.4, verification-*, logs) and source\Core\obj. Kept: the offline package and build caches (packages, nuget-cache, android-sdk, jdk, explorer-tools, installer-tools, pack-tools), the signing keys (android-keystore, explorer-signing), app\ and the last two versions in dist\.

Result: the project folder went from 10.3 GB to 6.1 GB (4.2 GB of build output removed); the repository lost 71 unused files.
