# NillyFiles Pro — Ultra-Light Native Windows 10/11 Tabbed & Dual-Pane File Explorer

> **Πανάλαφρος, ακαριαίος Native File Explorer για Windows 10 & 11 με Καρτέλες (Tabs) και Διπλό Παράθυρο (Dual-Pane) — χωρίς XAML/WinUI lag, χωρίς Electron/WebView2, 0 MB RAM όταν είναι κλειστό.**
> **Created by [Nilly graphics (Sotiris Christos)](https://nillygraphics.com/) · Part of the 0€ MIT [NillyGraphics Suite](https://github.com/nillynp/nillyhub)**

---

## Γιατί NillyFiles Pro; (Ultra-Light Σύγκριση)

| Χαρακτηριστικό | **NillyFiles Pro** | Windows 11 File Explorer |
|---|---|---|
| **Μέγεθος στον δίσκο** | **~65–85 KB EXE** (0 εξωτερικά DLL!) | WinUI / XAML / Cloud & AI shell modules |
| **RAM ανοιχτό** | **~14–24 MB** | 120–350+ MB (μεγαλώνει όσο ανοίγεις tabs) |
| **RAM κλειστό (Background)** | **0 MB · 0% CPU · 0% GPU** | Μόνιμο shell + cloud/indexing overhead |
| **Άνοιγμα φακέλου 10.000 αρχείων** | **Χιλιοστά δευτερολέπτου** (`FindFirstFileExW` + Virtual ListView) | Καθυστέρηση WinUI rendering |
| **Ασφάλεια συστήματος** | **100% Ασφαλές** — ΔΕΝ πειράζει ούτε απεγκαθιστά το `explorer.exe` | — |
| **Δικαιώματα / Κόστος** | **Zero Admin (HKCU) · 0€ MIT Open Source** | — |

---

## Κύριες Δυνατότητες / Key Features

- **Καρτέλες (Multi-Tab):** `Ctrl+T` νέα καρτέλα, `Ctrl+W` ή μεσαίο κλικ κλείσιμο, ανεξάρτητο ιστορικό Πίσω/Εμπρός (`Alt+Left` / `Alt+Right`) ανά καρτέλα.
- **Διπλό Παράθυρο 1 Κλικ (Dual-Pane · `F3`):** Δύο ανεξάρτητα παράθυρα δίπλα-δίπλα με `Tab` εναλλαγή, **`F5` Αντιγραφή** και **`F6` Μετακίνηση** στο απέναντι παράθυρο.
- **Win32 `FindFirstFileExW` + Virtual ListView:** Άμεση σάρωση και ομαλό scroll ακόμα και σε παλιά laptop με HDD/4 GB RAM.
- **Πραγματικό Windows Shell Clipboard (`CF_HDROP`):** `Ctrl+C` / `Ctrl+X` / `Ctrl+V` και Drag & Drop δουλεύουν αμφίδρομα με Windows Explorer, Desktop, Photoshop, Illustrator κ.ά.
- **Ασφαλής Διαγραφή στον Κάδο Ανακύκλωσης:** `Delete` στέλνει στον Κάδο (`SHFileOperation FOF_ALLOWUNDO`), `Shift+Delete` μόνο με ρητή επιβεβαίωση.
- **Άμεσο Φίλτρο (χωρίς τόνους Ελληνικά/Αγγλικά · `Ctrl+F`):** Φιλτράρισμα αρχείων σε `0 ms` καθώς πληκτρολογείς.
- **Γρήγορη Πρόσβαση, Αγαπημένα (`★`) & Μπάρες Δίσκων:** Άμεση εικόνα ελεύθερου χώρου GB για κάθε δίσκο.
- **Προαιρετική Προεπισκόπηση (`Alt+P`):** Εικόνες (διαστάσεις px + thumbnail χωρίς κλείδωμα αρχείου) & κείμενο/κώδικας — `0 MB` κόστος όταν είναι κλειστή.
- **Κουμπί `i` (Ultra-Light Specs):** Δείχνει ζωντανά το μέγεθος EXE (KB), την τρέχουσα RAM (MB) και όλες τις συντομεύσεις.
- **Dark & Light Monokai Θέμα**, Άνοιγμα PowerShell εδώ, Αντιγραφή διαδρομής (`Ctrl+Shift+C`), Native Ιδιότητες (`Alt+Enter`).

---

## Εγκατάσταση / Installation (Zero Admin)

1. Κατέβασε το `NillyFiles-Pro-v1.0-Portable.zip` από τα [Releases](https://github.com/nillynp/nillyfiles-pro/releases) (ή εγκατάστησέ το με 1 κλικ από το **[NillyHub](https://github.com/nillynp/nillyhub)**).
2. Κάνε διπλό κλικ στο `Install_NillyFilesPro.bat` — μεταγλωττίζει το native EXE τοπικά με τον ενσωματωμένο compiler των Windows (`csc.exe`, 0 εξωτερικά downloads) και φτιάχνει συντομεύσεις + δεξί κλικ «Άνοιγμα με NillyFiles Pro».
3. Για πλήρη αφαίρεση: `Uninstall_NillyFilesPro.bat`.

---

## License & Author

MIT License — Created by **Nilly graphics (Sotiris Christos)** · [https://nillygraphics.com/](https://nillygraphics.com/)
