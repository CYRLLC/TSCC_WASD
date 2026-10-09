# Code signing

Preview builds of YnyrWASD are **unsigned**. Windows SmartScreen may warn that the app is
from an unknown publisher. Until signing is in place, verify the ZIP's SHA-256 against the
`.sha256` file attached to the GitHub release before running it.

## Plan: SignPath Foundation (free for open source)

[SignPath Foundation](https://signpath.org/) signs open-source projects at no cost with a
certificate issued to the foundation, and builds the binaries from the public repository
in CI so the signature proves *what source* produced them. This fits YnyrWASD because the
release ZIP is already built by GitHub Actions from a tag.

Only the repository owner can apply, because SignPath reviews the project and its maintainers.

1. **Apply:** https://signpath.org/apply. The project must have an OSI license (MIT ✓),
   be publicly released, and describe what is signed. Mention that the app drives
   ViGEmBus and HidHide but installs no drivers itself.
2. **After approval**, SignPath creates an organization and project. In it:
   - Add a *GitHub trusted build system* for `CYRLLC/TSCC_WASD`.
   - Create an *artifact configuration* that signs `YnyrWASD.App.exe`, `YnyrWASD.App.dll`
     and `YnyrWASD.Core.dll` inside the ZIP.
   - Create a *signing policy* named `release-signing`.
3. **Repository secrets:** add `SIGNPATH_API_TOKEN` and the organization ID as
   `SIGNPATH_ORGANIZATION_ID` in the repository's Actions secrets.
4. **Workflow:** in `.github/workflows/release.yml`, between "Build, test and package" and
   "Create draft preview", upload the unsigned ZIP as an artifact and add the
   [`SignPath/github-action-submit-signing-request`](https://github.com/SignPath/github-action-submit-signing-request)
   step with `wait-for-completion: true`. Then replace the ZIP with the signed output and
   recompute the `.sha256`.
5. Update this file and the README once releases are signed.

## Alternatives

- **Azure Artifact Signing** (formerly Trusted Signing): inexpensive monthly plan; requires an
  identity-validated Azure account. It integrates with GitHub Actions through
  [`Azure/artifact-signing-action`](https://github.com/Azure/artifact-signing-action).
- **Self-signed certificates** do not remove SmartScreen warnings for other users and are
  not recommended for public releases.

Never commit certificates, private keys or signing tokens to the repository.
