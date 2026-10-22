# Guida per lo sviluppo

## Requisiti

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`brew install --cask dotnet-sdk` su macOS).
- Facoltativo: PowerShell 7 per gli script `build/*.ps1` (pacchetti Windows e Linux, download dei motori).

## Compilare ed eseguire

```bash
dotnet run --project src/Filee.App          # l'app, con dati e impostazioni normali
dotnet test                                  # tutti i test
build/build-trasmuta-macos.sh --install      # Trasmuta.app in ~/Applications
```

Per provare una build senza toccare le impostazioni di tutti i giorni, avviala con una cartella dati vuota:

```bash
FILEE_DATA_DIR=/tmp/trasmuta-prova dotnet run --project src/Filee.App
```

Con questa variabile la copia ha dati, motori e istanza propri e non registra voci di sistema.

## Pubblicare una versione

1. In `Directory.Build.props` imposta `<Version>` al nuovo numero, ad esempio `1.1.0`.
2. In `CHANGELOG.md` aggiungi in cima la sezione `## 1.1.0`, con il blocco nascosto `<!-- update … -->` che decide
   quanto insistente è l'avviso (le istruzioni sono all'inizio del file).
3. Fai commit e push su `main`, poi crea il tag:
   ```bash
   git tag v1.1.0 && git push origin v1.1.0
   ```
   Senza una copia locale del repository: Actions → **Release** → **Run workflow** sul ramo `main`, con **publish**
   spuntato. Il workflow crea da sé il tag `v<Version>` sul commit che ha compilato (si ferma se esiste già).

Il workflow **Release** (`.github/workflows/release.yml`) compila l'app per Apple Silicon e Intel su un Mac di
GitHub, scarica e verifica i motori inclusi, pubblica i `.dmg` (prima installazione) e gli zip (aggiornamento
automatico) con le impronte SHA-256 nella pagina della versione e ne usa la sezione del CHANGELOG come testo. Entro
un'ora le app aperte mostrano l'avviso e una notifica. Se tag e `<Version>`
non coincidono, o manca la sezione nel CHANGELOG, il workflow si ferma senza pubblicare nulla.

Per provare solo la compilazione: Actions → Release → **Run workflow**; `.dmg` e zip restano scaricabili come
artefatti. In locale `build/build-trasmuta-macos.sh --dmg` crea il `.dmg` (serve `pip install dmgbuild`); lo sfondo
della sua finestra si rigenera con `python3 build/tools/make-dmg-background.py`.

## Struttura

| Cartella | Contenuto |
|---|---|
| `src/Filee.Core` | Modello: preset, profili, impostazioni, coda di conversione, rilevamento formati. `AppIdentity.cs` definisce nome e identificativo dell'app. |
| `src/Filee.Engines` | I convertitori e i motori esterni (ImageMagick, FFmpeg, LibreOffice, …). |
| `src/Filee.App` | L'interfaccia Avalonia: finestre, pagine delle impostazioni, il menu di conversione (`Controls/DonutMenu*.cs`). |
| `src/Filee.Platform.*` | Integrazione con macOS, Windows e Linux (gesti, menu contestuali, avvio automatico). |
| `src/Filee.Cli` | Il comando da terminale. |
| `tests/` | Test unitari e di interfaccia (questi ultimi salvano schermate in `bin/…/screenshots`). |

I nomi `Filee.*` di cartelle, progetti e namespace sono interni e non compaiono nell'app.

## Il menu di conversione

Il controllo `DonutMenu` disegna tutti gli stili:

- `DonutMenu.cs` — cerchio, animazioni, input del puntatore.
- `DonutMenu.Layouts.cs` — gli stili Ventaglio, Elenco e Griglia e i tasti 1–9.
- `DonutMenu.Groups.cs` — gruppi di preset e tastiera.
- `DonutMenu.Flip.cs` — il retro del menu (<kbd>Tab</kbd> o rotella).
- `DonutMenu.Progress.cs` — l'anello di avanzamento dopo il rilascio.

Tutti gli stili rispondono agli hit test con gli stessi tipi (`DonutHit`), così finestra, controller e
trascinamento non dipendono dallo stile. `MenuLayoutTests` verifica ogni stile e ne salva una schermata.

## Testi e lingue

I testi sono in `src/Filee.App/Assets/i18n/<lingua>.json`; l'inglese (`en.json`) è il riferimento. Vedi
[Aggiungere una lingua](docs/ADDING-A-LANGUAGE.md).

## Stile del codice

Segui le impostazioni di `.editorconfig`. I commenti spiegano il perché, non il cosa.
