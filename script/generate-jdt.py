#!/usr/bin/env python3
"""
Script de génération du Journal de Travail (JDT) à partir des commits Git.
Le journal sera généré en Markdown et sauvegardé dans docs/JDT.md.
Les commits sont regroupés par semaine (définie en ISO-8601).
"""

import subprocess
import datetime
from collections import defaultdict
import os

def get_git_log():
    """
    Récupère l'historique Git sous forme de lignes formatées :
    date_iso|hash|message|timestamp
    - date_iso: date au format ISO (YYYY-MM-DD)
    - timestamp: epoch seconds (UTC) pour calculer les durées entre commits
    """
    result = subprocess.run(
        [
            "git", "log",
            "--pretty=format:%ad|%h|%s|%at",
            "--date=short",
        ],
        stdout=subprocess.PIPE,
        text=True,
        check=True,
    )
    return result.stdout.strip().splitlines()

def week_range(year, week):
    """
    Calcule la date de début (lundi) et la date de fin (dimanche) pour une semaine ISO donnée
    compatible avec Python < 3.8.
    """
    jan4 = datetime.date(year, 1, 4)
    jan4_weekday = jan4.isoweekday()
    start = jan4 - datetime.timedelta(days=jan4_weekday-1) + datetime.timedelta(weeks=week-1)
    end = start + datetime.timedelta(days=6)
    return start, end

def format_duration(seconds: int) -> str:
    """Formate une durée en secondes en chaîne lisible (ex: 1h 23m, 12m, 45s)."""
    if seconds is None or seconds <= 0:
        return ""
    m, s = divmod(seconds, 60)
    h, m = divmod(m, 60)
    parts = []
    if h:
        parts.append(f"{h}h")
    if m:
        parts.append(f"{m}m")
    if not parts and s:
        parts.append(f"{s}s")
    return " ".join(parts)

def format_commit(date_obj, commit_hash, message, duration_seconds):
    """Formate un commit pour Markdown avec la durée depuis le commit précédent."""
    dur = format_duration(duration_seconds)
    suffix = f" (Δ {dur})" if dur else ""
    return f"- **{date_obj}** [`{commit_hash}`]{suffix} : {message}"

def main():
    lines = get_git_log()
    commits_by_week = defaultdict(list)

    # Regroupement par année/semaine ISO
    for line in lines:
        parts = line.split("|")
        if len(parts) < 4:
            continue
        date_str, commit_hash, message, ts_str = parts
        try:
            date_obj = datetime.datetime.strptime(date_str, "%Y-%m-%d").date()
            ts = int(ts_str)
        except Exception:
            continue
        iso_year, iso_week, _ = date_obj.isocalendar()
        commits_by_week[(iso_year, iso_week)].append({
            "date": date_obj,
            "hash": commit_hash,
            "message": message,
            "ts": ts,
        })
    
    # Tri des semaines par ordre décroissant
    sorted_weeks = sorted(commits_by_week.keys(), reverse=True)
    output = []

    # En-tête principal
    today = datetime.date.today()
    output.append(f"# Journal de Travail (JDT)")
    output.append(f"*Généré le {today}*")
    output.append("\n---\n")

    for (year, week) in sorted_weeks:
        start, end = week_range(year, week)
        header = f"## Semaine {week} ({start} → {end}, {year})"
        output.append(header)
        output.append("")
        # Calcul des durées par commit (diff avec le commit précédent, tri ascendant par temps)
        week_commits = commits_by_week[(year, week)]
        ordered_by_time = sorted(week_commits, key=lambda c: c["ts"])  # plus ancien → plus récent
        durations = {}
        prev_ts = None
        for c in ordered_by_time:
            cur_ts = c["ts"]
            durations[c["hash"]] = (cur_ts - prev_ts) if prev_ts is not None else None
            prev_ts = cur_ts

        # Rendu en date décroissante (comme avant)
        commits = sorted(week_commits, key=lambda x: (x["date"], x["ts"]), reverse=True)
        for c in commits:
            output.append(
                format_commit(
                    c["date"],
                    c["hash"],
                    c["message"],
                    durations.get(c["hash"]) if durations else None,
                )
            )
        output.append("")  # Ligne vide pour séparer les semaines
        output.append("---")  # Séparateur visuel entre les semaines
        output.append("")

    # Création du dossier docs si nécessaire
    os.makedirs("docs", exist_ok=True)

    # Sauvegarde du journal dans docs/JDT.md
    with open("../docs/JDT.md", "w", encoding="utf-8") as f:
        f.write("\n".join(output))
    
    print("✅ Journal de Travail généré dans docs/JDT.md")

if __name__ == "__main__":
    main()
