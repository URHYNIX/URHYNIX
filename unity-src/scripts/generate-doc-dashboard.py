#!/usr/bin/env python3
"""Generate a derived HTML/JSON dashboard from canonical robotapp docs."""

from __future__ import annotations

import argparse
import html
import json
import re
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

try:
    from zoneinfo import ZoneInfo
except ImportError:  # pragma: no cover - Python 3.8 fallback
    ZoneInfo = None


STATUS_VALUES = {"open", "done", "locked", "stale", "unknown"}
FR5_SCOPE_BY_FILE = {
    "fr5-gripper-live-success-pattern.md": "gripper-only live control",
    "fr5-tiny-joint-live-success-pattern.md": "tiny joint MoveJ narrow path",
    "fr5-teaching-point-live-success-pattern.md": "teaching point one-shot live path",
    "fr5-connect-sync-debug-success-pattern.md": "connect + sync baseline",
}


@dataclass(frozen=True)
class SourceDoc:
    path: Path
    rel_path: str
    text: str
    frontmatter: dict[str, object]
    title: str
    last_updated: str | None
    status: str


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate docs/html dashboard artifacts from robotapp SSOT docs."
    )
    parser.add_argument("--docs-root", default="./docs", help="Docs root directory")
    parser.add_argument("--output-dir", default="./docs/html", help="Output directory")
    parser.add_argument("--git-root", default=".", help="Repository root")
    return parser.parse_args()


def kst_now() -> datetime:
    if ZoneInfo is not None:
        return datetime.now(ZoneInfo("Asia/Seoul"))
    return datetime.now(timezone.utc)


def read_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        return path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return ""


def parse_frontmatter(text: str) -> tuple[dict[str, object], str]:
    if not text.startswith("---\n"):
        return {}, text

    end = text.find("\n---", 4)
    if end == -1:
        return {}, text

    raw = text[4:end].strip()
    body = text[end + 4 :].lstrip("\n")
    allowed = {"title", "doc_type", "status", "domain", "canonical", "last_updated"}
    data: dict[str, object] = {}
    for line in raw.splitlines():
        if ":" not in line:
            continue
        key, value = line.split(":", 1)
        key = key.strip()
        if key not in allowed:
            continue
        value = value.strip().strip('"').strip("'")
        if value.lower() == "true":
            data[key] = True
        elif value.lower() == "false":
            data[key] = False
        else:
            data[key] = value
    return data, body


def first_heading(text: str) -> str | None:
    for line in text.splitlines():
        match = re.match(r"^#\s+(.+?)\s*$", line)
        if match:
            return match.group(1).strip()
    return None


def normalize_status(raw: str | None) -> str:
    if not raw:
        return "unknown"
    value = raw.strip().lower()
    if value in STATUS_VALUES:
        return value
    if value in {"complete", "completed", "qa", "green", "healthy", "active"}:
        return "done"
    if value in {"in_progress", "in-progress", "progress", "진행중"}:
        return "open"
    if value in {"hold", "blocked", "stuck"}:
        return "open"
    if value in {"lock", "out-of-scope", "excluded"}:
        return "locked"
    return "unknown"


def collect_source_paths(docs_root: Path) -> list[Path]:
    explicit = [
        docs_root / "status" / "ACTIVE-WORK-INDEX.md",
        docs_root / "status" / "FR5-LIVE-INTEGRATION-ROADMAP.md",
        docs_root / "ref" / "product" / "pendant-v3" / "progress-checklist.md",
        docs_root / "ref" / "product" / "pendant-v3" / "v3-componentization-priority-plan.md",
        docs_root / "status" / "AUTOMATION-HEALTH.md",
        docs_root / "status" / "NIGHTLY-RUN-LOG.md",
    ]
    success_patterns = sorted(
        (docs_root / "ref" / "product" / "roadmap").glob("fr5-*-success-pattern.md")
    )
    seen: set[Path] = set()
    paths: list[Path] = []
    for path in explicit + success_patterns:
        if path in seen or not path.exists():
            continue
        seen.add(path)
        paths.append(path)
    return paths


def load_source_doc(path: Path, git_root: Path) -> SourceDoc:
    text = read_text(path)
    frontmatter, body = parse_frontmatter(text)
    title = str(frontmatter.get("title") or first_heading(body) or path.stem)
    rel_path = path.relative_to(git_root).as_posix()
    status = normalize_status(str(frontmatter.get("status", "")))
    last_updated = frontmatter.get("last_updated")
    return SourceDoc(
        path=path,
        rel_path=rel_path,
        text=body,
        frontmatter=frontmatter,
        title=title,
        last_updated=str(last_updated) if last_updated else None,
        status=status,
    )


def source_docs_payload(source_docs: list[SourceDoc]) -> list[dict[str, object]]:
    payload: list[dict[str, object]] = []
    for doc in source_docs:
        item = {
            "path": doc.rel_path,
            "title": doc.title,
            "status": doc.status,
            "last_updated": doc.last_updated,
        }
        item.update(
            {
                key: value
                for key, value in doc.frontmatter.items()
                if key in {"doc_type", "domain", "canonical"}
            }
        )
        payload.append(item)
    return payload


def strip_markdown(value: str) -> str:
    value = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", value)
    value = value.replace("`", "")
    value = re.sub(r"\*\*(.*?)\*\*", r"\1", value)
    value = re.sub(r"\s+", " ", value)
    return value.strip()


def md_links(value: str) -> list[dict[str, str]]:
    links: list[dict[str, str]] = []
    for label, href in re.findall(r"\[([^\]]+)\]\(([^)]+)\)", value):
        links.append({"label": strip_markdown(label), "href": href})
    return links


def parse_table_rows(doc: SourceDoc) -> list[dict[str, object]]:
    rows: list[dict[str, object]] = []
    for line_no, line in enumerate(doc.text.splitlines(), start=1):
        stripped = line.strip()
        if not stripped.startswith("|") or "---" in stripped:
            continue
        cells = [cell.strip() for cell in stripped.strip("|").split("|")]
        if len(cells) < 2:
            continue
        first = strip_markdown(cells[0]).lower()
        second = strip_markdown(cells[1]).lower() if len(cells) > 1 else ""
        if first in {"상태", "status", "slice", "phase", "자동화"}:
            continue
        status = normalize_status(first)
        label = strip_markdown(cells[1])
        if status == "unknown" and second:
            second_status = normalize_status(second)
            if second_status != "unknown":
                status = second_status
                label = strip_markdown(cells[0])
                if len(cells) > 2 and strip_markdown(cells[2]):
                    label = f"{label}: {strip_markdown(cells[2])}"
        if status == "unknown" and first not in {"open", "locked", "done"}:
            continue
        rows.append(
            {
                "status": status,
                "label": label,
                "source": doc.rel_path,
                "line": line_no,
                "links": md_links(line),
                "raw_status": first,
            }
        )
    return rows


def parse_checkboxes(doc: SourceDoc) -> list[dict[str, object]]:
    rows: list[dict[str, object]] = []
    for line_no, line in enumerate(doc.text.splitlines(), start=1):
        match = re.match(r"^\s*[-*]\s+\[([ xX])\]\s+(.+)$", line)
        if not match:
            continue
        checked, label = match.groups()
        rows.append(
            {
                "status": "done" if checked.lower() == "x" else "open",
                "label": strip_markdown(label),
                "source": doc.rel_path,
                "line": line_no,
                "links": md_links(label),
            }
        )
    return rows


def parse_keyword_bullets(doc: SourceDoc) -> list[dict[str, object]]:
    keywords = re.compile(r"(open|blocked|남은|다음|후속|해야|pending)", re.IGNORECASE)
    rows: list[dict[str, object]] = []
    for line_no, line in enumerate(doc.text.splitlines(), start=1):
        stripped = line.strip()
        if not stripped.startswith("- "):
            continue
        if not keywords.search(stripped):
            continue
        label = strip_markdown(stripped[2:])
        if len(label) < 12:
            continue
        rows.append(
            {
                "status": "open",
                "label": label,
                "source": doc.rel_path,
                "line": line_no,
                "links": md_links(stripped),
            }
        )
    return rows


def collect_open_items(source_docs: list[SourceDoc]) -> list[dict[str, object]]:
    items: list[dict[str, object]] = []
    for doc in source_docs:
        if doc.rel_path.endswith("NIGHTLY-RUN-LOG.md"):
            continue
        for item in parse_table_rows(doc) + parse_checkboxes(doc) + parse_keyword_bullets(doc):
            if item["status"] == "open":
                items.append(item)
    return dedupe_items(items, limit=40)


def collect_locked_scope(source_docs: list[SourceDoc]) -> list[dict[str, object]]:
    locked: list[dict[str, object]] = []
    patterns = re.compile(r"(locked|lock|범위 밖|제외|not broad|broad .*아니|섞지|only|좁)", re.IGNORECASE)
    for doc in source_docs:
        for item in parse_table_rows(doc) + parse_checkboxes(doc):
            if item["status"] == "locked":
                locked.append(item)
        for line_no, line in enumerate(doc.text.splitlines(), start=1):
            stripped = line.strip()
            if not stripped.startswith("- ") or not patterns.search(stripped):
                continue
            label = strip_markdown(stripped[2:])
            if len(label) < 12:
                continue
            locked.append(
                {
                    "status": "locked",
                    "label": label,
                    "source": doc.rel_path,
                    "line": line_no,
                    "links": md_links(stripped),
                }
            )
    return dedupe_items(locked, limit=30)


def evidence_scope(doc: SourceDoc) -> str:
    name = doc.path.name
    if name in FR5_SCOPE_BY_FILE:
        return FR5_SCOPE_BY_FILE[name]
    if "FR5-LIVE" in name or "ACTIVE-WORK" in name:
        return "FR5 live current status"
    if "progress-checklist" in name:
        return "Pendant V3 progress"
    return "doc evidence"


def collect_latest_verified_evidence(source_docs: list[SourceDoc]) -> list[dict[str, object]]:
    keywords = re.compile(r"(green|verified|success|성공|확인|수렴|readback|evidence|artifact)", re.IGNORECASE)
    skip = re.compile(r"(Success Pattern|Purpose|목적|이 문서는|goal)", re.IGNORECASE)
    evidence: list[dict[str, object]] = []
    for doc in source_docs:
        if doc.rel_path.endswith("NIGHTLY-RUN-LOG.md"):
            continue
        for line_no, line in enumerate(doc.text.splitlines(), start=1):
            stripped = line.strip()
            if not stripped.startswith("- ") or not keywords.search(stripped) or skip.search(stripped):
                continue
            label = strip_markdown(stripped[2:])
            if len(label) < 18:
                continue
            evidence.append(
                {
                    "status": "done",
                    "scope": evidence_scope(doc),
                    "label": label,
                    "source": doc.rel_path,
                    "line": line_no,
                    "links": md_links(stripped),
                }
            )
    return select_evidence(evidence, limit=35, per_scope_limit=8)


def evidence_priority(item: dict[str, object]) -> tuple[int, str]:
    source = str(item.get("source", ""))
    if Path(source).name in FR5_SCOPE_BY_FILE:
        return (0, source)
    if "pendant-v3/progress-checklist.md" in source:
        return (1, source)
    if "ACTIVE-WORK-INDEX.md" in source:
        return (2, source)
    return (3, source)


def select_evidence(
    items: Iterable[dict[str, object]], limit: int, per_scope_limit: int
) -> list[dict[str, object]]:
    selected: list[dict[str, object]] = []
    seen: set[tuple[str, str]] = set()
    scope_counts: dict[str, int] = {}
    for item in sorted(items, key=evidence_priority):
        label = str(item.get("label", "")).strip()
        source = str(item.get("source", ""))
        scope = str(item.get("scope", "doc evidence"))
        key = (source, label)
        if not label or key in seen or scope_counts.get(scope, 0) >= per_scope_limit:
            continue
        seen.add(key)
        scope_counts[scope] = scope_counts.get(scope, 0) + 1
        selected.append(item)
        if len(selected) >= limit:
            break
    return selected


def collect_active_tracks(source_docs: list[SourceDoc]) -> list[dict[str, object]]:
    active = next((doc for doc in source_docs if doc.rel_path.endswith("ACTIVE-WORK-INDEX.md")), None)
    if active is None:
        return []

    tracks: list[dict[str, object]] = []
    current: dict[str, object] | None = None
    for line_no, line in enumerate(active.text.splitlines(), start=1):
        heading = re.match(r"^###\s+\d+\.\s+(.+?)\s*$", line)
        if heading:
            current = {
                "name": strip_markdown(heading.group(1)),
                "status": "unknown",
                "summary": "",
                "source": active.rel_path,
                "line": line_no,
            }
            tracks.append(current)
            continue
        if current is None:
            continue
        status = re.match(r"^-\s+상태:\s+`?([^`]+)`?", line.strip())
        if status:
            normalized = normalize_status(status.group(1))
            current["status"] = normalized if normalized != "unknown" else "open"
            current["summary"] = strip_markdown(status.group(1))
    return tracks


def extract_dates(text: str) -> list[str]:
    return re.findall(r"20\d{2}-\d{2}-\d{2}", text)


def collect_automation_health(source_docs: list[SourceDoc], today: str) -> dict[str, object]:
    doc = next((item for item in source_docs if item.rel_path.endswith("AUTOMATION-HEALTH.md")), None)
    if doc is None:
        return {"status": "unknown", "source": None, "latest_run": None, "automations": []}

    dates = extract_dates(doc.text)
    latest_run = max(dates) if dates else None
    status = "done" if latest_run == today else "stale"
    automations: list[dict[str, object]] = []
    for row in parse_table_rows(doc):
        automations.append(
            {
                "name": row["label"],
                "status": "stale" if status == "stale" else normalize_status(str(row.get("raw_status"))),
                "source": doc.rel_path,
                "line": row["line"],
            }
        )
    return {
        "status": status,
        "source": doc.rel_path,
        "latest_run": latest_run,
        "today": today,
        "automations": automations,
        "warning": None
        if status == "done"
        else "AUTOMATION-HEALTH.md is older than today's KST date; HEALTHY text is treated as stale.",
    }


def collect_stale_docs(source_docs: list[SourceDoc], automation_health: dict[str, object]) -> list[dict[str, object]]:
    stale: list[dict[str, object]] = []
    if automation_health.get("status") == "stale":
        stale.append(
            {
                "status": "stale",
                "label": automation_health.get("warning"),
                "source": automation_health.get("source"),
                "latest_run": automation_health.get("latest_run"),
            }
        )
    for doc in source_docs:
        if doc.status == "unknown":
            stale.append(
                {
                    "status": "unknown",
                    "label": f"{doc.title} has no normalized frontmatter status",
                    "source": doc.rel_path,
                    "last_updated": doc.last_updated,
                }
            )
    return stale


def collect_next_actions(open_items: list[dict[str, object]]) -> list[dict[str, object]]:
    next_actions: list[dict[str, object]] = []
    preferred_sources = (
        "docs/status/ACTIVE-WORK-INDEX.md",
        "docs/status/FR5-LIVE-INTEGRATION-ROADMAP.md",
        "docs/ref/product/pendant-v3/v3-componentization-priority-plan.md",
    )
    for source in preferred_sources:
        for item in open_items:
            if item.get("source") == source:
                next_actions.append(item)
            if len(next_actions) >= 8:
                return next_actions
    return next_actions


def dedupe_items(items: Iterable[dict[str, object]], limit: int) -> list[dict[str, object]]:
    seen: set[tuple[str, str]] = set()
    output: list[dict[str, object]] = []
    for item in items:
        label = str(item.get("label", "")).strip()
        source = str(item.get("source", ""))
        key = (source, label)
        if not label or key in seen:
            continue
        seen.add(key)
        output.append(item)
        if len(output) >= limit:
            break
    return output


def build_dashboard_data(source_docs: list[SourceDoc]) -> dict[str, object]:
    now = kst_now()
    today = now.date().isoformat()
    open_items = collect_open_items(source_docs)
    automation_health = collect_automation_health(source_docs, today)
    return {
        "generated_at": now.isoformat(timespec="seconds"),
        "generated_timezone": "Asia/Seoul",
        "source_docs": source_docs_payload(source_docs),
        "active_tracks": collect_active_tracks(source_docs),
        "open_items": open_items,
        "locked_scope": collect_locked_scope(source_docs),
        "latest_verified_evidence": collect_latest_verified_evidence(source_docs),
        "automation_health": automation_health,
        "stale_docs": collect_stale_docs(source_docs, automation_health),
        "next_actions": collect_next_actions(open_items),
    }


def build_project_structure(data: dict[str, object]) -> dict[str, object]:
    return {
        "generated_at": data["generated_at"],
        "generated_timezone": data["generated_timezone"],
        "session_context": {
            "purpose": "Derived short context for robotapp docs; canonical truth remains in docs/status and docs/ref.",
            "read_first": [
                "docs/status/ACTIVE-WORK-INDEX.md",
                "docs/status/FR5-LIVE-INTEGRATION-ROADMAP.md",
                "docs/ref/product/ux/robotcontrol-next-session-handoff.md",
            ],
            "ssot_rule": "Do not treat docs/html as source of truth; regenerate it from canonical markdown.",
        },
        "active_tracks": data["active_tracks"],
        "open_items": list(data["open_items"])[:12],
        "locked_scope": list(data["locked_scope"])[:12],
        "latest_verified_evidence": list(data["latest_verified_evidence"])[:12],
        "automation_health": data["automation_health"],
        "next_actions": list(data["next_actions"])[:8],
    }


def write_json(path: Path, payload: dict[str, object]) -> None:
    path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2, sort_keys=False) + "\n",
        encoding="utf-8",
    )


def file_link(source: object, line: object | None = None) -> str:
    if not source:
        return "#"
    href = str(source)
    if href.startswith("docs/"):
        href = "../" + href.removeprefix("docs/")
    if line:
        href = f"{href}#L{line}"
    return html.escape(href)


def render_items(items: Iterable[dict[str, object]], empty: str) -> str:
    cards: list[str] = []
    for item in items:
        label = html.escape(str(item.get("label", "")))
        status = html.escape(str(item.get("status", "unknown")))
        source = html.escape(str(item.get("source", "")))
        line = item.get("line")
        scope = item.get("scope")
        scope_html = f'<span class="scope">{html.escape(str(scope))}</span>' if scope else ""
        cards.append(
            f"""
            <article class="item">
              <div class="item-top">
                <span class="badge badge-{status}">{status}</span>
                {scope_html}
              </div>
              <p>{label}</p>
              <a href="{file_link(source, line)}">{source}</a>
            </article>
            """
        )
    if not cards:
        return f'<p class="empty">{html.escape(empty)}</p>'
    return "\n".join(cards)


def render_tracks(tracks: Iterable[dict[str, object]]) -> str:
    cards: list[str] = []
    for track in tracks:
        name = html.escape(str(track.get("name", "")))
        status = html.escape(str(track.get("status", "unknown")))
        summary = html.escape(str(track.get("summary", "")))
        source = html.escape(str(track.get("source", "")))
        line = track.get("line")
        cards.append(
            f"""
            <article class="track">
              <span class="badge badge-{status}">{status}</span>
              <h3>{name}</h3>
              <p>{summary or "Status is inferred from the canonical active work index."}</p>
              <a href="{file_link(source, line)}">{source}</a>
            </article>
            """
        )
    return "\n".join(cards) if cards else '<p class="empty">No active tracks parsed.</p>'


def build_html(data: dict[str, object]) -> str:
    generated_at = html.escape(str(data["generated_at"]))
    automation = data["automation_health"]
    automation_status = html.escape(str(automation.get("status", "unknown")))
    automation_warning = automation.get("warning") or "Automation health is current."
    return f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="icon" href="data:,">
  <title>Robotapp Docs Dashboard</title>
  <style>
    :root {{
      color-scheme: light;
      --bg: #f6f7f2;
      --panel: #ffffff;
      --ink: #17211d;
      --muted: #63706a;
      --line: #d8ded6;
      --open: #b24d1f;
      --done: #236e4a;
      --locked: #504f60;
      --stale: #9a6a08;
      --unknown: #697179;
      --accent: #0f6d7a;
    }}
    * {{ box-sizing: border-box; }}
    body {{
      margin: 0;
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      color: var(--ink);
      background: var(--bg);
    }}
    header {{
      padding: 28px 32px 20px;
      border-bottom: 1px solid var(--line);
      background: #eef3ee;
    }}
    h1 {{ margin: 0 0 8px; font-size: 28px; letter-spacing: 0; }}
    header p {{ margin: 0; color: var(--muted); max-width: 900px; line-height: 1.5; }}
    main {{ padding: 24px 32px 40px; max-width: 1280px; margin: 0 auto; }}
    .tabs {{ display: flex; gap: 8px; flex-wrap: wrap; margin-bottom: 20px; }}
    .tab-button {{
      border: 1px solid var(--line);
      background: var(--panel);
      color: var(--ink);
      padding: 9px 14px;
      border-radius: 6px;
      font-weight: 650;
      cursor: pointer;
    }}
    .tab-button[aria-selected="true"] {{
      border-color: var(--accent);
      color: #fff;
      background: var(--accent);
    }}
    .tab-panel {{ display: none; }}
    .tab-panel.active {{ display: block; }}
    .summary-grid, .item-grid {{
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
      gap: 12px;
      margin: 14px 0 24px;
    }}
    .track, .item, .warning {{
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: 8px;
      padding: 14px;
      min-height: 112px;
    }}
    h2 {{ margin: 24px 0 10px; font-size: 18px; letter-spacing: 0; }}
    h3 {{ margin: 10px 0 8px; font-size: 17px; letter-spacing: 0; }}
    p {{ line-height: 1.48; }}
    .item p {{ margin: 10px 0 12px; }}
    a {{ color: var(--accent); text-decoration: none; overflow-wrap: anywhere; }}
    a:hover {{ text-decoration: underline; }}
    .badge {{
      display: inline-flex;
      align-items: center;
      height: 22px;
      padding: 0 8px;
      border-radius: 999px;
      color: #fff;
      font-size: 12px;
      font-weight: 750;
      text-transform: uppercase;
    }}
    .badge-open {{ background: var(--open); }}
    .badge-done {{ background: var(--done); }}
    .badge-locked {{ background: var(--locked); }}
    .badge-stale {{ background: var(--stale); }}
    .badge-unknown {{ background: var(--unknown); }}
    .item-top {{ display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }}
    .scope {{ color: var(--muted); font-size: 13px; }}
    .warning {{ border-color: #d5b464; background: #fff8df; }}
    .empty {{ color: var(--muted); }}
    footer {{ color: var(--muted); margin-top: 28px; font-size: 13px; }}
  </style>
</head>
<body>
  <header>
    <h1>Robotapp Docs Dashboard</h1>
    <p>Generated artifact from canonical markdown. Source of truth remains in <code>docs/status</code> and <code>docs/ref</code>. Generated at {generated_at}.</p>
  </header>
  <main>
    <nav class="tabs" aria-label="Dashboard tabs">
      <button class="tab-button" aria-selected="true" data-tab="current">현재 작업</button>
      <button class="tab-button" aria-selected="false" data-tab="fr5">FR5 Live</button>
      <button class="tab-button" aria-selected="false" data-tab="pendant">Pendant V3</button>
    </nav>

    <section id="current" class="tab-panel active">
      <h2>Active Tracks</h2>
      <div class="summary-grid">{render_tracks(data["active_tracks"])}</div>
      <h2>Next Actions</h2>
      <div class="item-grid">{render_items(data["next_actions"], "No next actions parsed.")}</div>
      <h2>Automation Warning</h2>
      <article class="warning">
        <span class="badge badge-{automation_status}">{automation_status}</span>
        <p>{html.escape(str(automation_warning))}</p>
      </article>
    </section>

    <section id="fr5" class="tab-panel">
      <h2>Open Items</h2>
      <div class="item-grid">{render_items([item for item in data["open_items"] if "fr5" in str(item.get("source", "")).lower() or "ACTIVE-WORK" in str(item.get("source", ""))], "No FR5 open items parsed.")}</div>
      <h2>Locked Scope</h2>
      <div class="item-grid">{render_items([item for item in data["locked_scope"] if "fr5" in str(item.get("source", "")).lower() or "ACTIVE-WORK" in str(item.get("source", ""))], "No FR5 locked scope parsed.")}</div>
      <h2>Latest Verified Evidence</h2>
      <div class="item-grid">{render_items([item for item in data["latest_verified_evidence"] if "FR5" in str(item.get("scope", "")) or "fr5" in str(item.get("source", "")).lower()], "No FR5 evidence parsed.")}</div>
    </section>

    <section id="pendant" class="tab-panel">
      <h2>Open Items</h2>
      <div class="item-grid">{render_items([item for item in data["open_items"] if "pendant-v3" in str(item.get("source", ""))], "No Pendant V3 open items parsed.")}</div>
      <h2>Locked Scope</h2>
      <div class="item-grid">{render_items([item for item in data["locked_scope"] if "pendant-v3" in str(item.get("source", ""))], "No Pendant V3 locked scope parsed.")}</div>
      <h2>Latest Verified Evidence</h2>
      <div class="item-grid">{render_items([item for item in data["latest_verified_evidence"] if "Pendant" in str(item.get("scope", "")) or "pendant-v3" in str(item.get("source", ""))], "No Pendant V3 evidence parsed.")}</div>
    </section>

    <footer>
      Generated files: <code>dashboard-data.json</code>, <code>project-structure.json</code>, <code>index.html</code>.
    </footer>
  </main>
  <script>
    for (const button of document.querySelectorAll('.tab-button')) {{
      button.addEventListener('click', () => {{
        const tab = button.dataset.tab;
        for (const other of document.querySelectorAll('.tab-button')) {{
          other.setAttribute('aria-selected', String(other === button));
        }}
        for (const panel of document.querySelectorAll('.tab-panel')) {{
          panel.classList.toggle('active', panel.id === tab);
        }}
      }});
    }}
  </script>
</body>
</html>
"""


def main() -> int:
    args = parse_args()
    git_root = Path(args.git_root).resolve()
    docs_root = (git_root / args.docs_root).resolve()
    output_dir = (git_root / args.output_dir).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    source_paths = collect_source_paths(docs_root)
    source_docs = [load_source_doc(path, git_root) for path in source_paths]
    data = build_dashboard_data(source_docs)

    write_json(output_dir / "dashboard-data.json", data)
    write_json(output_dir / "project-structure.json", build_project_structure(data))
    (output_dir / "index.html").write_text(build_html(data), encoding="utf-8")

    print(f"Generated {output_dir / 'dashboard-data.json'}")
    print(f"Generated {output_dir / 'project-structure.json'}")
    print(f"Generated {output_dir / 'index.html'}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
