# Installare e aggiornare Trasmuta

| Sistema | Disponibile |
|---|---|
| **macOS 13 o successivo** — Apple Silicon (M1, M2, M3, M4, …) e Intel | ✅ Sì |
| **Windows 10 / 11** | ⏳ Non ancora |
| **Linux** (x64, ARM64) | ⏳ Non ancora |

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

---

## Windows e Linux

Il codice di Trasmuta funziona anche su Windows e Linux, ma le versioni pronte da scaricare per questi sistemi non sono
ancora pubblicate. Quando lo saranno compariranno nella stessa pagina
[Releases](https://github.com/Pietro310/Trasmuta/releases/latest) e questa guida verrà aggiornata con i passaggi.
