#!/usr/bin/env python3
"""Generate news.json (repo info + latest release) for the home page mirror."""
import datetime
import json
import os
import urllib.request

OWNER = "MEIMAOA"
REPOS = ["ShutdownTimer-Edu", "ShutdownTimer", "SysKit", "SnakeLauncher", "SnakeGame"]
TOKEN = os.environ.get("GH_TOKEN", "")


def get(url):
    headers = {
        "User-Agent": "news-bot",
        "Accept": "application/vnd.github+json",
    }
    if TOKEN:
        headers["Authorization"] = f"Bearer {TOKEN}"
    req = urllib.request.Request(url, headers=headers)
    with urllib.request.urlopen(req, timeout=30) as resp:
        return json.load(resp)


def main():
    repos = []
    for name in REPOS:
        try:
            d = get(f"https://api.github.com/repos/{OWNER}/{name}")
        except Exception as e:  # noqa: BLE001
            print("skip", name, e)
            continue
        try:
            rel = get(f"https://api.github.com/repos/{OWNER}/{name}/releases/latest")
        except Exception:  # noqa: BLE001
            rel = {}

        repos.append({
            "full_name": d.get("full_name"),
            "name": d.get("name"),
            "description": d.get("description") or "",
            "stars": d.get("stargazers_count", 0),
            "forks": d.get("forks_count", 0),
            "language": d.get("language") or "",
            "html_url": d.get("html_url"),
            "owner_avatar_url": (d.get("owner") or {}).get("avatar_url") or "",
            "updated_at": d.get("updated_at"),
            "latest_release_tag": rel.get("tag_name"),
            "latest_release_name": rel.get("name"),
            "release_notes": rel.get("body"),
            "released_at": rel.get("published_at"),
        })

    data = {
        "updated_at": datetime.datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ"),
        "repos": repos,
    }
    with open("news.json", "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    print("wrote", len(repos), "repos")


if __name__ == "__main__":
    main()
