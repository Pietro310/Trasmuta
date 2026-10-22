# Novità

<!--
Ogni versione ha una sezione "## X.Y.Z": il workflow Release la usa come testo della pagina della versione su GitHub, e
l'app la mostra nell'avviso di aggiornamento. Il blocco nascosto "update" (invisibile su GitHub) dice quanto insistere:
  level: required     l'avviso compare a ogni avvio finché non si aggiorna
  level: recommended  l'avviso compare una volta per versione (predefinito)
  level: optional     nessun avviso, solo il pulsante nella barra laterale
  en: …               una riga di riassunto per l'avviso (in inglese, come l'interfaccia); se ne possono mettere più
-->

## 1.0.1

<!-- update
level: recommended
en: Clearer update steps on macOS: download the zip and replace the app in Applications.
-->

- L'avviso di aggiornamento su macOS spiega i passaggi giusti: scaricare lo zip, uscire da Trasmuta e sostituire l'app
  in Applicazioni.
- Nuova [guida all'installazione e all'aggiornamento](docs/INSTALLAZIONE.md).
- Nuova icona.

## 1.0.0

<!-- update
level: recommended
en: Four menu styles: Ring, Fan, List and Grid (Theme → Menu style).
en: Keys 1–9 pick a format in every style.
-->

Prima versione di Trasmuta.

- **Stili del menu**: oltre al Cerchio, i nuovi stili Ventaglio, Elenco e Griglia (Theme → Menu
  style), con anteprima dal vivo. In Elenco e Griglia l'intestazione resta sotto il cursore e i gruppi di preset si
  aprono in una colonna laterale.
- **Tasti 1–9** per scegliere un formato in qualsiasi stile; nella Griglia le frecce su e giù cambiano riga, nell'Elenco
  la freccia destra apre un gruppo e quella sinistra lo chiude.
- Nuova identità: nome, icona e cartelle dati proprie (`~/Library/Application Support/Trasmuta`).
- Script `build/build-trasmuta-macos.sh` per compilare, firmare e installare l'app su macOS.
- Versioni pubblicate automaticamente su GitHub (Apple Silicon e Intel) e avvisi di aggiornamento nell'app
  (About → Updates).
