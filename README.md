# <img width="64" height="64" alt="Vapor" src="https://github.com/user-attachments/assets/ff6e149d-cb88-4806-8c28-5f047728281e" /> Vapor

[![NuGet Version](https://img.shields.io/badge/nuget-v1.0.0-blue.svg)](https://www.nuget.org/)
[![Framework](https://img.shields.io/badge/.NET%20Standard-2.0-green.svg)](https://learn.microsoft.com/en-us/dotnet/standard/net-standard)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)]()
[![License](https://img.shields.io/badge/license-CC%20BY--SA%201.0-orange.svg)]()

**Vapor** è un framework IPC (Inter-Process Communication) Full-Duplex ad altissime prestazioni e bassissima latenza per .NET. Sfruttando i **Memory-Mapped Files (MMF)** e i semafori nativi del sistema operativo, Vapor permette a due o più processi separati di scambiarsi pacchetti di byte direttamente nella RAM condivisa, garantendo un sovraccarico della CPU dello **0% a riposo** e un livello di allocazioni prossimo allo zero.

Nato per supportare ecosistemi complessi e sensibili alla latenza (come tool di gestione stile *LocalAdmin* per server dedicati, bot di integrazione o microservizi locali), Vapor supera i limiti prestazionali e i ritardi tipici dei tradizionali Socket TCP o WebSockets locali.

---

## 🏗️ Architettura & Performance

Vapor divide la comunicazione in due canali monodirezionali paralleli strutturati su un **RingBuffer (Buffer Circolare)** binario non gestito. Questo previene i rallentamenti dovuti al Garbage Collector (GC) e azzera i conflitti di lettura/scrittura.

### Struttura dell'Header di Memoria (Primi 8 Byte)
Ogni canale alloca un segmento fisso per tracciare i puntatori in modo atomico:
* **Offset `0x00` (4 byte):** `WritePosition` — Indica dove il mittente scriverà il prossimo blocco.
* **Offset `0x04` (4 byte):** `ReadPosition` — Indica fino a dove il destinatario ha completato la lettura.
* **Offset `0x08`+ :** `DataBuffer` — Lo spazio circolare effettivo in cui scorrono i messaggi (Lunghezza + Payload).

La sincronizzazione tra i processi è gestita tramite `EventWaitHandle` cross-process: il thread di ascolto viene congelato a livello di kernel del sistema operativo e si risveglia istantaneamente solo quando viene notificato un nuovo pacchetto.

---

## 🛠️ Installazione

Il core della libreria è sviluppato in **.NET Standard 2.0** per garantire la massima compatibilità cross-runtime (supporta sia .NET 8.0+ che ambienti server-side restrittivi o legacy).

Assicurati che il tuo progetto referenzi il pacchetto di supporto per la memoria nel file `.csproj`:
```xml
<ItemGroup>
    <PackageReference Include="System.Memory" Version="4.5.5" />
</ItemGroup>
