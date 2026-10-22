# Installare e aggiornare Trasmuta

| Sistema | Disponibile |
|---|---|
| **macOS 13 o successivo** — Apple Silicon (M1, M2, M3, M4, …) e Intel | ✅ Sì |
| **Windows 10 (1809) / 11** — x64 | ✅ Sì, dalla 1.0.4 |
| **Linux** — x64 e ARM64, Ubuntu 22.04 o successivo (glibc 2.35) | ✅ Sì, dalla 1.0.4 |

Vai a: [macOS](#macos) · [Windows](#windows) · [Linux](#linux) · [Aggiornare](#aggiornare) ·
[Disinstallare](#disinstallare)

---

## macOS

### 1. Scopri che Mac hai

Menu  → **Informazioni su questo Mac**. Alla voce **Chip** (o **Processore**):

- c'è scritto **Apple M1, M2, M3, M4…** → ti serve il file che finisce con **`osx-arm64.dmg`**
- c'è scritto **Intel** → ti serve il file che finisce con **`osx-x64.dmg`**

(Gli zip con lo stesso nome contengono la stessa app: servono all'aggiornamento automatico, ma puoi usarli anche per
installare.)

### 2. Scarica

Apri la pagina dell'ultima versione:
**https://github.com/Pietro310/Trasmuta/releases/latest**

In fondo, nella sezione **Assets**, clicca sul file giusto per il tuo Mac, ad esempio `Trasmuta-1.0.4-osx-arm64.dmg`.
Lo trovi nella cartella **Download**.

### 3. Installa

1. Fai doppio clic sul file `.dmg`: si apre una finestra con **Trasmuta** e la cartella **Applicazioni**.
2. Trascina Trasmuta sulla cartella **Applicazioni**, poi chiudi la finestra ed espelli il disco "Trasmuta" (⏏ nella
   barra laterale del Finder).

Hai aperto Trasmuta direttamente da Download? All'avvio ti propone **Sposta in Applicazioni**: accetta e si sposta e
si riapre da sola. Da Download non potrebbe aggiornarsi, perché macOS la esegue da una copia di sola lettura.

### 4. Primo avvio

Trasmuta non è distribuita tramite l'App Store né firmata da Apple, quindi al primo avvio macOS la blocca. Si sblocca
una volta sola:

1. Apri **Trasmuta** da Applicazioni. Compare un avviso che non può essere aperta: premi **Fine** (o **OK**).
2. Apri **Impostazioni di Sistema → Privacy e sicurezza** e scorri fino in fondo.
3. Accanto a "Trasmuta è stata bloccata…" premi **Apri comunque** e conferma con la password o Touch ID.

Da quel momento si apre normalmente.

### 5. Permessi

Trasmuta li chiede la prima volta che servono. Senza, il menu sotto il cursore non si apre.

| Permesso | Dove | A cosa serve |
|---|---|---|
| **Accessibilità** | Impostazioni di Sistema → Privacy e sicurezza → Accessibilità → attiva **Trasmuta** | Riconoscere il gesto (tasto + trascinamento) e le scorciatoie |
| **Automazione → Finder** | Compare come richiesta: rispondi **OK** | Leggere i file selezionati nel Finder quando usi la scorciatoia da tastiera |

Dopo aver attivato l'Accessibilità, se il menu non compare esci da Trasmuta (icona nella barra dei menu → Esci) e
riaprila.

### 6. Prova

Tieni premuto **Option ⌥**, trascina un file dal Finder e lascialo su un formato del menu che compare sotto il
cursore. Il file convertito viene salvato accanto all'originale.

Lo stile del menu (Cerchio, Ventaglio, Elenco, Griglia) si sceglie in **Tema → Stile menu** (Theme → Menu style).

---

## Windows

### 1. Scarica

Dalla pagina **https://github.com/Pietro310/Trasmuta/releases/latest**, nella sezione **Assets**, scarica
`Trasmuta-<versione>-win-Setup.exe`.

(C'è anche `Trasmuta-<versione>-win-Portable.zip`, la versione portatile che non si installa: estraila in una
cartella e avvia `Trasmuta.exe`. Non si aggiorna da sola.)

### 2. Installa

1. Apri il file scaricato. Trasmuta non è firmata con un certificato, quindi Windows può mostrare **"PC protetto da
   Windows"**: premi **Ulteriori informazioni** e poi **Esegui comunque**.
2. Scegli la cartella e le opzioni: voce **Converti con Trasmuta** nel menu del tasto destro (su Windows 11 anche nel
   menu principale), avvio con Windows, comando `trasmuta` nel terminale, icona sul desktop.
3. Alla fine Trasmuta si apre. Si installa accanto a un eventuale Filee, senza toccarlo.

### 3. Prova

Tieni premuto **Ctrl**, trascina un file da Esplora file e lascialo su un formato del menu che compare sotto il
cursore. Oppure tasto destro sul file → **Converti con Trasmuta** (su Windows 11, se non l'hai aggiunta al menu
principale, la voce è in **Mostra altre opzioni**).

---

## Linux

### 1. Scarica

Dalla pagina **https://github.com/Pietro310/Trasmuta/releases/latest** scarica `Trasmuta-<versione>-linux-x64.tar.gz`
(PC Intel/AMD) o `Trasmuta-<versione>-linux-arm64.tar.gz` (ARM a 64 bit). Se non sai quale, nel terminale `uname -m`
risponde `x86_64` (→ x64) o `aarch64` (→ arm64).

### 2. Installa le librerie di sistema

Il runtime .NET è incluso, ma servono alcune librerie. Su Ubuntu 24.04:

```bash
sudo apt-get install ca-certificates libc6 libgcc-s1 libgssapi-krb5-2 libicu74 libssl3t64 libstdc++6 tzdata zlib1g \
  libfontconfig1 libfreetype6 libx11-6 libice6 libsm6 libxrandr2 libxkbcommon0 libxi6 libxtst6 libxt6 fonts-noto-cjk python3
```

Su Ubuntu 22.04 sostituisci `libicu74` con `libicu70` e `libssl3t64` con `libssl3`. Per le altre distribuzioni vedi
`INSTALL.txt` dentro l'archivio.

### 3. Installa

```bash
tar -xzf Trasmuta-*-linux-x64.tar.gz
cd Trasmuta-*-linux-x64
./install.sh
```

Non serve root: Trasmuta va nella tua cartella utente, compare tra le applicazioni e il comando `trasmuta` va in
`~/.local/bin`. Per provarla senza installarla, avvia `./Trasmuta` dalla cartella estratta.

### 4. Prova

Il gesto (**Ctrl** + trascinamento) funziona in una sessione **X11** (o XWayland). In una sessione Wayland usa la
finestra in cui trascinare i file o le azioni del file manager; per KDE Plasma 6 c'è un'opzione sperimentale descritta
in `INSTALL.txt`.

---

## Aggiornare

Trasmuta controlla da sola se è uscita una versione nuova: all'avvio, poi ogni ora, quando apri la finestra e quando il
Mac si risveglia. Te lo dice con un avviso in basso a destra, una notifica di macOS (la prima volta macOS chiede se
permetterle), un pallino sull'icona nella barra dei menu e nel Dock, e un pulsante nella barra laterale. Puoi anche controllare a mano in **Informazioni → Aggiornamenti → Controlla ora** (About → Updates → Check now).

Per aggiornare premi **Aggiorna** nell'avviso, nella barra laterale o in **Informazioni → Aggiornamenti**.
Trasmuta scarica la nuova versione, ne verifica l'integrità, si chiude e si riapre aggiornata dopo pochi secondi.
Non serve sbloccarla di nuovo in Privacy e sicurezza, e impostazioni, preset e permessi restano quelli di prima.

### Aggiornare a mano

Se Trasmuta non può aggiornarsi da sola, l'avviso spiega perché:

- **è in Download o su un'immagine disco**: il pulsante diventa **Sposta e aggiorna**. Trasmuta si sposta in
  Applicazioni, si riapre e si aggiorna;
- **il tuo account non può modificare la cartella Applicazioni** (non sei amministratore): segui i passaggi qui sotto;
  macOS chiederà la password di un amministratore.

Aggiornare a mano serve anche se l'aggiornamento automatico non riesce, o se hai la versione 1.0.1 o precedente
(l'aggiornamento automatico è arrivato con la 1.0.2):

1. Apri https://github.com/Pietro310/Trasmuta/releases/latest e scarica il `.dmg` per il tuo Mac, come al punto 2
   dell'installazione.
2. **Esci da Trasmuta**: icona nella barra dei menu → **Esci**.
3. Fai doppio clic sul `.dmg` e trascina la nuova **Trasmuta** su **Applicazioni**. Alla domanda scegli **Sostituisci**.
4. Riaprila. macOS la blocca come al primo avvio: sbloccala allo stesso modo (punto 4 dell'installazione).

Impostazioni, preset e permessi restano quelli di prima.

**Windows**: premi **Aggiorna** nell'avviso: Trasmuta scarica il nuovo Setup, ne verifica l'integrità e lo avvia; si
chiude, si aggiorna e si riapre. La versione portatile apre invece la pagina di download: estrai il nuovo zip sopra la
vecchia cartella.

**Linux**: l'avviso apre la pagina di download. Scarica il nuovo archivio, esegui `./uninstall.sh` dalla cartella della
versione installata (impostazioni e file convertiti restano) e poi `./install.sh` da quella nuova.

Non vuoi gli avvisi? Disattivali in **Informazioni → Aggiornamenti**, spegnendo **Controlla automaticamente gli aggiornamenti**.

### Verificare il download (facoltativo)

Ogni versione pubblica anche il file `Trasmuta-<versione>-SHA256SUMS.txt`. Per controllare che il `.dmg` o lo zip
siano integri, nel Terminale, dalla cartella Download:

```bash
shasum -a 256 -c --ignore-missing Trasmuta-*-SHA256SUMS.txt
```

Deve rispondere `OK` accanto al nome del file.

---

## Disinstallare

**Windows**: Impostazioni → App → App installate → **Trasmuta** → Disinstalla.

**Linux**: `./uninstall.sh` dalla cartella estratta (toglie anche i motori scaricati da Trasmuta; impostazioni e file
convertiti restano).

**macOS**:

1. Esci da Trasmuta e trascina **Trasmuta** da Applicazioni nel Cestino.
2. Per eliminare anche impostazioni e motori scaricati, nel Finder premi ⇧⌘G, scrivi
   `~/Library/Application Support/Trasmuta` e sposta la cartella nel Cestino.
3. In Impostazioni di Sistema → Privacy e sicurezza → Accessibilità, seleziona Trasmuta e premi **–**.

---

## Problemi frequenti

**Il menu non compare quando trascino un file.**
Controlla che Trasmuta sia attiva in Privacy e sicurezza → **Accessibilità**. Se lo è già ma non funziona, rimuovila
con **–**, aggiungila di nuovo con **+** (da Applicazioni) e riavvia Trasmuta. In alternativa, nel Terminale:

```bash
tccutil reset Accessibility com.trasmuta.app
```

poi riapri Trasmuta e concedi di nuovo il permesso.

**"Trasmuta è danneggiata e non può essere aperta".**
Succede se il download si è interrotto o lo zip è stato aperto da un'app diversa da Archive Utility. Scarica di nuovo
il `.dmg` e aprilo con un doppio clic nel Finder.

**Alcuni formati sono grigi nel menu.**
Servono un motore aggiuntivo (ad esempio FFmpeg per video e audio, LibreOffice per alcuni documenti). Passa con il
mouse sul formato per sapere quale, e scaricalo nella pagina **Motori**.
