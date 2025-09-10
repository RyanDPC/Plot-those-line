---
title: "Rapport de Projet"
author: "De Pina Correia Ryan – CID3A"
date: "Lausanne, Vennes – 32p "
project_manager: "Melly Jonathan"
---

<p align="center" style="font-size:22px; font-weight:bold;">
ETML Informatique
</p>

---

# Rapport de Projet

_Auteur : Nom – Classe_  
_Lieu – Durée_  
_Chef de projet : Nom_  
_Mandant : Nom et adresse_

---

# Table des matières
- [1. Spécifications](#1-spécifications)
  - [1.1 Titre](#11-titre)
    - [Description](#12-description)
  - [1.3 Matériel et logiciels à disposition](#13-matériel-et-logiciels-à-disposition)
  - [1.4 Prérequis](#14-prérequis)
  - [1.5 Cahier des charges](#15-cahier-des-charges)
    - [1.5.1 Objectifs et portée (SMART)](#151-objectifs-et-portée-smart)
    - [1.5.2 Caractéristiques des utilisateurs et impacts](#152-caractéristiques-des-utilisateurs-et-impacts)
    - [1.5.3 Fonctionnalités requises](#153-fonctionnalités-requises)
    - [1.5.4 Contraintes](#154-contraintes)
    - [1.5.5 Travail à réaliser par l'apprenti](#155-travail-à-réaliser-par-lapprenti)
    - [1.5.6 Si le temps le permet](#156-si-le-temps-le-permet)
    - [1.5.7 Méthodes de validation](#157-méthodes-de-validation)
  - [1.6 Points évalués](#16-points-évalués)
  - [1.7 Validation et conditions de réussite](#17-validation-et-conditions-de-réussite)
- [2. Planification Initiale](#2-planification-initiale)
- [3. Analyse](#3-analyse)
- [4. Réalisation](#4-réalisation)
- [5. Tests](#5-tests)
- [6. Conclusion](#6-conclusion)
- [7. Divers](#7-divers)
- [8. Annexes](#8-annexes)

---

# 1. Spécifications

## 1.1 Plot-Those-Line 
### Description
Concevoir un logiciel permettant d’afficher et d’analyser des séries temporelles sous forme graphique.  
L’utilisateur pourra importer des données externes (CSV, JSON, API) et comparer plusieurs jeux de données simultanément.  

## 1.3 Matériel et logiciels à disposition  

- **Visual Studio 2022**
- **GitHub**
  - Project
  - Repository (repo)
  - Roadmap
  - User stories
- **Python**
- **API : Alpha Vantage**
- **Figma**

## 1.4 Prérequis  
- **ICT-323 Programmation fonctionnelle**
- **C#**
- **API REST (HttpClient)**
- **Gestion de projet**
- **Passion Lecture (mobile)**
- **C335 (flashcards)**

## 1.5 Cahier des charges

### Gestion de projet
Les directives spécifiques à la gestion de projet vous seront fournies séparément par votre chef de projet.  
Dans tous les cas, les points suivants doivent être respectés :

1. Vous êtes inscrit au projet **P_FUN** sur Marketplace :  
   [Lien vers Marketplace](https://apps.pm2etml.ch/jobDefinitions/102)

2. Vous devez effectuer une **analyse sous forme de User Stories (US)**  
   - Inclure les tests d’acceptance  
   - Inclure des maquettes  
   - Cette étape doit être réalisée avant de commencer à coder

3. Vous devez fournir une **planification simple** de la réalisation de ces US avant de commencer à coder

4. Vous devez tenir un **Journal de travail [Jdt.md]()**

---

### 1.5.1 Objectifs et portée (SMART)  

#### Objectif 1 : Réaliser un programme informatique de qualité
- Organisé (namespace, classes, commit log, …)  
- Compacté (pas de copié/collé)  
- Optimisé (structures adaptées)  
- Testé (tests unitaires)  
- Commenté  
- Complet (code, modèle de données, maquettes PDF, exécutable, …)  

#### Objectif 2 : Prouver que vous êtes digne de confiance dans la gestion d’un projet
- Journal de travail à jour  
- Pro-activité (poser des questions au client, faire des démonstrations, utiliser Git pour versioning)  

---

### 1.5.2 Caractéristiques des utilisateurs et impacts  
- Public cible : utilisateurs ayant besoin d’analyser des données temporelles (finance, météo, santé, sport, etc.).  
- Impacts attendus :  
  - Gain de temps pour la comparaison de données.  
  - Accessibilité via un outil ergonomique.  
  - Flexibilité d’affichage des graphiques pour répondre à des besoins variés.  

---

### 1.5.3 Fonctionnalités requises  
- Afficher plusieurs séries temporelles simultanément.  
- Offrir une flexibilité d’affichage pour analyser les données en détail.  
- Importer des séries de données de manière permanente (CSV, JSON, API).  
- Comparer plusieurs intervalles de temps pour une même donnée.  
- Mode fonctions permettant d’afficher des expressions mathématiques (préconfigurées et personnalisées via Roslyn).  

---

### 1.5.4 Contraintes  
- Utiliser **LINQ** (pas de boucle `for`).  
- Implémenter **au moins 2 extensions C#**.  
- Utiliser une **librairie graphique** (Forms, MAUI, Uno, WPF, …).  
- Utiliser une **librairie de visualisation de données** (ex. [ScottPlot](https://scottplot.net/)).  

---

### 1.5.5 Travail à réaliser par l'apprenti  
- Réalisation du code source et des fonctionnalités principales.  
- Mise en place des tests unitaires et de validation.  
- Rédaction du rapport PDF (introduction, planification, tests, journal de travail, usage de l’IA, conclusion).  
- Publication sur GitHub (release incluant code + rapport).  

---

### 1.5.6 Si le temps le permet  
- Ajouter des options avancées d’affichage (filtres, zoom interactif, mise en forme personnalisée).  
- Développer des connecteurs supplémentaires vers d’autres APIs publiques.  
- Optimiser les performances pour de très grands jeux de données.  

---

### 1.5.7 Méthodes de validation  
- Vérification par tests unitaires et manuels.  
- Démonstrations intermédiaires au client/chef de projet.  
- Validation des User Stories (tests d’acceptance).  
- Comparaison avec les spécifications initiales.  

---

## 1.6 Points évalués  
- Respect des **objectifs SMART**.  
- Qualité et organisation du **code source**.  
- Capacité à utiliser **GitHub** pour la gestion de projet.  
- Exhaustivité et régularité du **journal de travail**.  
- Pertinence de la **planification** et respect des délais.  
- Clarté et qualité du **rapport PDF**.  
- Autonomie, pro-activité et communication avec le client.  

---

## 1.7 Validation et conditions de réussite  
- Le projet est considéré comme réussi si :  
  - Une **release GitHub** est disponible avec le code complet et fonctionnel.  
  - Le **rapport PDF** contient toutes les sections requises (introduction, planification, tests, journal, IA, conclusion).  
  - Les fonctionnalités principales (affichage de séries temporelles, importation, flexibilité d’affichage, mode fonctions) sont opérationnelles.  
  - Les **tests unitaires** confirment le bon fonctionnement des composants critiques.  
  - Le **journal de travail** est tenu régulièrement et reflète l’évolution du projet.  
  - Le projet respecte les contraintes techniques et la planification validée avec le client.  

---

# 2. Planification Initiale  
_A compléter_

---

# 3. Analyse

## 3.1 Opportunités  
_A compléter_

## 3.2 Document d’analyse et conception  
_A compléter_

## 3.3 Conception des tests  
_A compléter_

## 3.4 Planification détaillée  
_A compléter_

---

# 4. Réalisation

## 4.1 Dossier de réalisation  
_A compléter_

## 4.2 Modifications  
_A compléter_

---

# 5. Tests

## 5.1 Dossier des tests  
_A compléter_

---

# 6. Conclusion

## 6.1 Bilan des fonctionnalités demandées  
_A compléter_

## 6.2 Bilan de la planification  
_A compléter_

## 6.3 Bilan personnel  
_A compléter_

---

# 7. Divers

## 7.1 Journal de travail  
_A compléter_

## 7.2 Bibliographie  
- Auteur, *Titre*, Édition, Année.  
- ...

## 7.3 Webographie  
- [Nom du site](https://) – Description  
- ...

---

# 8. Annexes  
_A compléter_
