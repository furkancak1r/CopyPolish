# Repository Guidelines

## Project Structure & Module Organization
- `main.py`: Windows tray app; hotkeys, clipboard, OpenRouter calls, settings UI.
- `requirements.txt`: Python dependencies.
- `CopyPolish.spec`: PyInstaller spec for a GUI, one-file build.
- `build.bat`: Convenience script to produce `dist/CopyPolish.exe`.
- `dist/`, `build/`: PyInstaller outputs. Treat as generated artifacts; do not edit.
- `README.md`: User-facing setup and usage notes.

## Build, Test, and Development Commands
- Create env: `python -m venv .venv && .venv\\Scripts\\activate`
- Install deps: `pip install -r requirements.txt`
- Run locally: `python main.py`
- Build exe (spec): `pyinstaller CopyPolish.spec`
- Build exe (one-liner): `pyinstaller --onefile --windowed --name CopyPolish main.py` or `./build.bat`
- Optional format (not enforced): `pip install black isort && black . && isort .`

## Coding Style & Naming Conventions
- Python 3.10+; 4-space indentation; follow PEP 8.
- Naming: `snake_case` for functions/variables, `UPPER_SNAKE_CASE` for constants (e.g., `APP_NAME`).
- Keep Windows compatibility (keyboard, toasts, tray). Avoid adding non-Windows-only bindings unless guarded.
- Persist config at `%APPDATA%/CopyPolish/config.json`; store API keys via `keyring`; never hardcode secrets.
- Keep UI strings succinct; align with existing Turkish labels.

## Testing Guidelines
- Framework: `pytest` if/when tests are added.
- Location & names: `tests/` with files like `test_main.py`, `test_*.py`.
- Mock network: do not hit OpenRouter in tests; stub `requests` and functions like `rewrite_text`/`translate_text_tr_en`.
- Run: `pytest -q` (after `pip install pytest`). Add smoke steps to README for manual validation (hotkeys, paste, notifications).

## Commit & Pull Request Guidelines
- Commits: imperative, present-tense subject under 72 chars, or Conventional Commits (e.g., `feat: add TR→EN translate hotkey`).
- Scope small and focused; keep unrelated refactors separate.
- PRs must include: summary, linked issue, local run steps, screenshots/GIFs of tray/menu/settings if UI changes, risk/rollback notes.
- Do not commit secrets. Prefer excluding `dist/` and `build/` from PRs unless updating a release.
- Update `README.md` when changing shortcuts, settings, or build steps.

## Security & Configuration Tips
- API key is retrieved from `keyring`; never log it. Optional envs: `OPENROUTER_SITE_URL`, `OPENROUTER_SITE_NAME`.
- Validate hotkey changes to avoid OS/app conflicts; prefer `CTRL+ALT+Y` / `CTRL+ALT+T` defaults.
