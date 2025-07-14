<img src="https://r2cdn.perplexity.ai/pplx-full-logo-primary-dark%402x.png" class="logo" width="120"/>

# Produktanforderungsdokument (PRD)

## 1. Einleitung

Dieses Dokument beschreibt die Anforderungen für das System zur Verwaltung und Bearbeitung von Gutachtenaufträgen. Es richtet sich an die beteiligten Rollen: **rvPuR**, **rvGutachten** und **Gutachter**.

## 2. Personas

| Persona | Beschreibung |
| :-- | :-- |
| rvPuR | Initiator von Gutachtenaufträgen, erstellt und verwaltet Aufträge. |
| rvGutachten | Verwaltung und Koordination der Gutachtenprozesse. |
| Gutachter | Externer Experte, der Gutachten erstellt und einreicht. |

## 3. Anforderungen nach Prozessschritt

### 3.1 Auftragserstellung und -verwaltung

- **rvPuR** kann einen neuen Gutachtenauftrag erstellen.
    - Es werden sowohl ein rvPuR- als auch ein rvSMD-Auftrag angelegt.
    - Der zuständige Gutachter wird ausgewählt.
    - Anlagen werden zu einem Gesamtdokument zusammengeführt und dem Auftrag beigefügt.


### 3.2 Benachrichtigung

- **rvGutachten** benachrichtigt den ausgewählten Gutachter über den neuen Auftrag und den Abgabetermin.


### 3.3 Auftragsevaluation

- **Gutachter** prüft den Auftrag und die Unterlagen.
    - Bei Annahme beginnt die Erstellung des Gutachtens.
    - Bei Ablehnung erfolgt keine weitere Bearbeitung.


### 3.4 Gutachtenerstellung

- **Gutachter** oder Mitarbeitende erstellen das Gutachten.
- Nach Fertigstellung wird das Gutachten eingereicht und signiert.


### 3.5 Abschluss und Löschung

- **rvGutachten** prüft das eingereichte Gutachten auf Vollständigkeit und Kriterienerfüllung.
- Nach erfolgreichem Abschluss wird das Gutachten vier Wochen nach Abschluss zusammen mit dem Auftrag und den Unterlagen gelöscht.


### 3.6 Mahnwesen

- **rvGutachten** mahnt den Gutachter bei Überschreitung des Abgabetermins.


### 3.7 Dokumentenmanagement

- **rvGutachten** kann während der Gutachtenerstellung neue Dokumente nachreichen.


### 3.8 Stornierung

- **rvGutachten** kann einen Auftrag stornieren, wenn kein Bedarf mehr besteht.
    - Der stornierte Auftrag bleibt vier Wochen sichtbar.
    - Die zugehörigen Unterlagen werden gelöscht.


### 3.9 Kommunikation und Rückfragen

- **Gutachter** kann während der Erstellung Rückfragen stellen.
- **rvGutachten** beantwortet Rückfragen zeitnah.


### 3.10 Abrechnung

- **Gutachter** stellt nach Abschluss des Gutachtens eine Rechnung an **rvGutachten**.


### 3.11 Stellungnahmen

- **rvGutachten** kann bei unklaren Gutachten eine zusätzliche Stellungnahme anfordern.
- **Gutachter** reicht die Stellungnahme zeitnah ein.


## 4. Funktionale Anforderungen (Zusammenfassung)

- Auftragsanlage mit Dokumentenzusammenführung
- Auswahl und Benachrichtigung des Gutachters
- Verwaltung von Fristen und Mahnwesen
- Möglichkeit zur Nachreichung von Dokumenten
- Stornierungsfunktion mit zeitlich begrenzter Sichtbarkeit
- Rückfrage- und Antwortfunktion zwischen Gutachter und rvGutachten
- Einreichung, Signatur und Abschluss von Gutachten
- Rechnungsstellung und Verwaltung von Stellungnahmen


## 5. Nicht-funktionale Anforderungen

- **Datensicherheit:** Schutz sensibler Daten und Dokumente
- **Benutzerfreundlichkeit:** Intuitive Bedienung für alle Rollen
- **Nachvollziehbarkeit:** Protokollierung aller Bearbeitungsschritte
- **Fristenmanagement:** Automatische Erinnerungen und Mahnungen


## 6. Akzeptanzkriterien

- Jeder Prozessschritt ist für die jeweilige Rolle klar erkennbar und ausführbar.
- Dokumente und Gutachten können jederzeit dem richtigen Auftrag zugeordnet werden.
- Benachrichtigungen und Fristen funktionieren zuverlässig.
- Rückfragen und Stellungnahmen sind nachvollziehbar dokumentiert.
- Lösch- und Archivierungsprozesse erfolgen automatisiert nach Ablauf der Fristen.

Dieses PRD bildet die Grundlage für die weitere technische und fachliche Ausarbeitung des Systems.

