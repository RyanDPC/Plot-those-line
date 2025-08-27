#!/usr/bin/env python3
"""
Script de génération du Journal de Travail (JDT) à partir des commits Git.
Le journal sera généré en Markdown et sauvegardé dans docs/JDT.md.
Les commits sont regroupés par semaine (définie en ISO-8601).
"""

import subprocess
import datetime
from collections import defaultdict

def get_git_log():
    """
    Récupère l'historique Git sous forme de lignes formatées :
    date|hash|message
    """
    result = subprocess.run(
        ["git", "log", "--pretty=format:%ad|%h|%s", "--date=short"],
        stdout=subprocess.PIPE,
        text=True,
        check=True
    )
    return result.stdout.strip().splitlines()

def week_range(year, week):
    """
    Calcule la date de début (lundi) et la date de fin (dimanche) pour une semaine ISO donnée.
    """
    start = datetime.date.fromisocalendar(year, week, 1)
    end = start + datetime.timedelta(days=6)
    return start, end

def main():
    lines = get_git_log()
    commits_by_week = defaultdict(list)

    # Traitement de chaque commit et regroupement par année/semaine ISO
    for line in lines:
        parts = line.split("|")
        if len(parts) < 3:
            continue
        date_str, commit_hash, message = parts
        try:
            date_obj = datetime.datetime.strptime(date_str, "%Y-%m-%d").date()
        except Exception as e:
            continue
        iso_year, iso_week, _ = date_obj.isocalendar()
        commits_by_week[(iso_year, iso_week)].append((date_obj, commit_hash, message))
    
    # Tri des semaines par ordre décroissant (les plus récentes en premier)
    sorted_weeks = sorted(commits_by_week.keys(), reverse=True)
    output = []

    for (year, week) in sorted_weeks:
        start, end = week_range(year, week)
        header = f"### Semaine du {start} au {end} (Semaine {week}, {year})"
        output.append(header)
        output.append("")
        # Tri des commits par date décroissante dans la semaine
        commits = sorted(commits_by_week[(year, week)], key=lambda x: x[0], reverse=True)
        for date_obj, commit_hash, message in commits:
            output.append(f"- **{date_obj}** - `{commit_hash}` : {message}")
        output.append("")  # Ligne vide pour séparer les sections
    
    # Sauvegarde du journal dans docs/JDT.md
    with open("docs/JDT.md", "w", encoding="utf-8") as f:
        f.write("\n".join(output))
    
    print("Journal de Travail généré dans docs/JDT.md")

if __name__ == "__main__":
    main()
