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
