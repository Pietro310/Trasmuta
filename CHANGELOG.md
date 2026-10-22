# Novità

<!--
Ogni versione ha una sezione "## X.Y.Z": il workflow Release la usa come testo della pagina della versione su GitHub, e
l'app la mostra nell'avviso di aggiornamento. Il blocco nascosto "update" (invisibile su GitHub) dice quanto insistere:
  level: required     l'avviso compare a ogni avvio finché non si aggiorna
  level: recommended  l'avviso compare una volta per versione (predefinito)
  level: optional     nessun avviso, solo il pulsante nella barra laterale
  en: …               una riga di riassunto per l'avviso (in inglese, come l'interfaccia); se ne possono mettere più
-->

## 1.0.4

<!-- update
level: recommended
en: Opened from Downloads, Trasmuta offers to move itself to Applications, where it can update itself.
en: New versions are noticed sooner (every hour and when the Mac wakes), with a system notification and a dot on the menu bar icon.
it: Se è aperta da Download, Trasmuta propone di spostarsi in Applicazioni, dove può aggiornarsi da sola.
it: Le nuove versioni vengono notate prima (ogni ora e al risveglio del Mac), con una notifica di sistema e un pallino sull'icona nella barra dei menu.
-->

- **Spostamento in Applicazioni**: se Trasmuta è aperta da Download (o da un'immagine disco), all'avvio propone di
  spostarsi in Applicazioni e si riapre da lì. Da Download macOS la esegue da una copia di sola lettura e non può
  aggiornarsi da sola.
- **Aggiornamenti notati prima**: il controllo avviene ogni ora (prima ogni 6), all'apertura della finestra e al
  risveglio del Mac. Quando esce una versione nuova arriva una **notifica di macOS**, e l'icona nella barra dei menu e
  quella nel Dock mostrano un pallino.
- **Avviso più chiaro quando non può aggiornarsi da sola**: invece di aprire GitHub senza spiegazioni, l'avviso dice
  perché. Se è in Download il pulsante diventa **Sposta e aggiorna** (si sposta in Applicazioni, si riapre e si
  aggiorna); se il tuo account non può modificare Applicazioni, spiega come aggiornarla a mano.
- **Nuovo file .dmg** per la prima installazione: si apre una finestra in cui basta trascinare Trasmuta su
  Applicazioni. Gli zip restano per l'aggiornamento automatico.

## 1.0.3

<!-- update
level: recommended
en: Fixes Trasmuta closing on its own when the app is moved or replaced while it runs.
-->

- Trasmuta non si chiude più da sola se l'app viene spostata o sostituita mentre è aperta: un errore nella scrittura
  del registro non può più interromperla.

## 1.0.2

<!-- update
level: recommended
en: Updates install themselves: press Update and Trasmuta restarts on the new version.
en: Trasmuta now speaks Italian.
-->

- **Aggiornamento con un clic**: premi **Aggiorna** e Trasmuta scarica la nuova versione, ne verifica l'integrità, si
  sostituisce e si riapre da sola. Niente più zip da trascinare né sblocco in Privacy e sicurezza.
- **Interfaccia in italiano**, scelta automaticamente sui Mac in italiano (si cambia in Generali → Lingua).

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

- **Stili del menu**: oltre al Cerchio, i nuovi stili Ventaglio, Elenco e Griglia (Tema → Stile
  menu), con anteprima dal vivo. In Elenco e Griglia l'intestazione resta sotto il cursore e i gruppi di preset si
  aprono in una colonna laterale.
- **Tasti 1–9** per scegliere un formato in qualsiasi stile; nella Griglia le frecce su e giù cambiano riga, nell'Elenco
  la freccia destra apre un gruppo e quella sinistra lo chiude.
- Nuova identità: nome, icona e cartelle dati proprie (`~/Library/Application Support/Trasmuta`).
- Script `build/build-trasmuta-macos.sh` per compilare, firmare e installare l'app su macOS.
- Versioni pubblicate automaticamente su GitHub (Apple Silicon e Intel) e avvisi di aggiornamento nell'app
  (Informazioni → Aggiornamenti).
