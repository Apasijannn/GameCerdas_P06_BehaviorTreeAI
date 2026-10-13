# Script Video Demo — Praktikum 6 Game Cerdas
## Behavior Tree & Utility-Based AI

**Project:** `GameCerdas_P06_BehaviorTreeAI` · **Scene:** `P06_BehaviorTree` · **Engine:** Unity 6
**Durasi total:** ± 7 menit (minimal 5 menit) · **Pembicara:** 3 orang

| Bagian | Pembicara | Waktu | Isi |
|---|---|---|---|
| 1 | Orang 1 — *[Nama 1]* | 00:00 – 02:20 | Pembukaan, environment, framework Behavior Tree |
| 2 | Orang 2 — *[Nama 2]* | 02:20 – 04:50 | Perception, Patrol, Chase, FOV, Obstacle, Attack, Cooldown |
| 3 | Orang 3 — *[Nama 3]* | 04:50 – 07:15 | Flee, eksperimen Utility AI, perbandingan, bonus, penutup |

> Teks **"Narasi"** = yang diucapkan. Teks **[LAYAR]** = yang ditampilkan/dilakukan saat itu.

---

## ✅ Persiapan Sebelum Merekam

1. **Pakai Behavior Tree** di kedua enemy:
   pilih `AggressiveEnemy` dan `CowardEnemy` → **centang `EnemyBTController`**, **uncheck `EnemyUtilityController`** dan `EnemyHybridController` (jika ada). Simpan scene (Ctrl+S).
2. Tab **Game** → slider **Scale = 1x**, resolusi rekaman **1920×1080**.
3. Tab **Scene** → tombol **Gizmos aktif** (untuk menampilkan Vision Range, Attack Range, FOV).
4. Tutup aplikasi lain, aktifkan *Do Not Disturb*. Rekam dengan OBS (Display Capture) + mic.
5. Siapkan di VS Code (tab terpisah): `BTNode.cs`, `SelectorNode.cs`, `SequenceNode.cs`, `CooldownDecorator.cs`, `EnemyBTController.cs`, `EnemyUtilityController.cs`.
6. Hafalkan kontrol:

| Tombol | Fungsi |
|---|---|
| W A S D | Gerak Player (jalan; otomatis lari saat dikejar) |
| Scroll / Z, X | Zoom kamera in-game |
| Tab | Ganti fokus kamera (Player → AggressiveEnemy → CowardEnemy) |
| R | Kembali ke tampilan seluruh arena |
| F1 | Sembunyikan/tampilkan panel BT Debugger |

**Parameter penting (dari komponen `EnemyPersonality`):**

| | AggressiveEnemy (merah) | CowardEnemy (biru) |
|---|---|---|
| Vision Range / Angle | 15 / 150° | 10 / 90° |
| Attack Range | 2.5 | 2 |
| Attack Cooldown | 0.8 s | 2 s |
| Low Health Threshold (Flee) | ≤ 15 | ≤ 70 |

---

## 🎙️ BAGIAN 1 — Orang 1 *(00:00 – 02:20)*
**Topik: Pembukaan, Environment, Framework Behavior Tree**

### 00:00 – 00:25 · Pembukaan & Identitas
**[LAYAR]** Slide/tampilan judul: *Praktikum 6 — Behavior Tree & Utility-Based AI*, nama + NRP ketiga anggota.

**Narasi:**
> "Assalamu'alaikum / Halo semuanya. Kami dari kelompok *[nama kelompok]*. Saya *[Nama 1, NRP]*, bersama *[Nama 2, NRP]* dan *[Nama 3, NRP]*. Pada video ini kami akan mendemokan Praktikum 6 Game Cerdas, yaitu **NPC Enemy berbasis Behavior Tree** yang dapat memilih perilaku Patrol, Chase, Attack, dan Flee, serta eksperimen **Utility-Based AI**."

### 00:25 – 01:00 · Tujuan & Environment
**[LAYAR]** Unity, tab **Scene**. Putar kamera mengelilingi arena. Tunjuk satu per satu di Hierarchy: `Environment → Ground`, `Obstacles (Wall01–03)`, `Props` (pilar & crate), `PatrolPoints (Waypoint01–04)`, `SafePoint`, `Player`, `AggressiveEnemy`, `CowardEnemy`.

**Narasi:**
> "Tujuan praktikum ini adalah membuat enemy yang memilih perilaku berdasarkan kondisi player, perception, jarak, dan health. Ini arena yang kami buat: ada **Ground**, **tembok dan obstacle** berlayer *Obstacle*, **empat waypoint** untuk rute patrol yang ditandai cincin cyan, dan **SafePoint** berupa bunker hijau sebagai tujuan saat enemy kabur. Kami juga memakai dua enemy dengan kepribadian berbeda: **Aggressive** berwarna merah dan **Coward** berwarna biru."

### 01:00 – 01:20 · NavMesh & NavMeshAgent
**[LAYAR]** Pilih `Environment → Navigation` → tampilkan **NavMeshSurface** dan overlay NavMesh biru. Lalu pilih `CowardEnemy` → tampilkan komponen **NavMeshAgent**.

**Narasi:**
> "Area jalan dibuat dengan **NavMeshSurface** dari package AI Navigation. Terlihat obstacle membuat lubang di NavMesh sehingga enemy tidak bisa menembusnya. Setiap enemy memiliki **NavMeshAgent** yang dipakai untuk Patrol, Chase, dan Flee melalui `SetDestination()`."

### 01:20 – 02:00 · Framework Behavior Tree
**[LAYAR]** VS Code: tampilkan berurutan `BTNode.cs` → `SelectorNode.cs` → `SequenceNode.cs` → `CooldownDecorator.cs`. Highlight bagian `Tick()`.

**Narasi:**
> "Framework Behavior Tree kami terdiri dari **BTNode** sebagai base class dengan tiga status: **Success, Failure, dan Running**.
> **Selector** mencoba child dari atas ke bawah dan berhenti di child pertama yang Success atau Running — ini yang membuat urutan child menjadi prioritas.
> **Sequence** menjalankan child berurutan dan langsung gagal jika satu child Failure — cocok untuk 'kondisi lalu aksi'.
> **ConditionNode** memeriksa kondisi, **ActionNode** menjalankan aksi, dan **CooldownDecorator** membungkus Attack agar tidak terjadi setiap frame."

### 02:00 – 02:20 · Struktur Tree & Prioritas
**[LAYAR]** `EnemyBTController.cs` → fungsi `BuildBehaviorTree()`. Bisa juga tampilkan diagram tree di slide.

**Narasi:**
> "Di `BuildBehaviorTree()`, tree disusun dengan root **Selector** berisi: Flee Sequence, Attack Sequence, Chase Sequence, lalu Patrol. Jadi prioritasnya **Flee > Attack > Chase > Patrol**. Kami juga menambahkan node **Search** sebelum Patrol sebagai pengembangan. Selanjutnya *[Nama 2]* akan mendemokan perception dan perilaku enemy."

---

## 🎙️ BAGIAN 2 — Orang 2 *(02:20 – 04:50)*
**Topik: Perception, Patrol, Chase, FOV, Obstacle, Attack, Cooldown**

### 02:20 – 02:50 · Sistem Perception
**[LAYAR]** VS Code: `EnemyBTController.cs` → fungsi `CanSeePlayer()`. Lalu Unity Scene view, pilih `CowardEnemy` → tunjukkan Gizmos lingkaran **Vision Range**, **Attack Range**, dan dua garis **FOV**.

**Narasi:**
> "Perception dilakukan di fungsi `CanSeePlayer()` dengan tiga pengecekan. Pertama **jarak**: player harus di dalam Vision Range. Kedua **Field of View**: sudut antara arah depan enemy dan player harus di bawah setengah Vision Angle. Ketiga **Raycast** ke layer Obstacle: kalau ada tembok di antaranya, player dianggap tidak terlihat. Di Scene view terlihat Gizmos untuk vision range, attack range, dan batas FOV."

### 02:50 – 03:15 · Demo PATROL
**[LAYAR]** Tekan **Play**. Player diam jauh dari enemy. Arahkan perhatian ke panel **BT Debugger** di kiri atas (Current Action = PATROL) dan cincin waypoint.

**Narasi:**
> "Saat Play, player berada jauh dan tidak terlihat. Kondisi `CanSeePlayer` gagal, sehingga Attack dan Chase gagal, dan Selector jatuh ke **Patrol**. Di panel BT Debugger terlihat Current Action **PATROL**, dan enemy berjalan dari Waypoint01 sampai Waypoint04 lalu kembali lagi."

### 03:15 – 03:40 · Demo CHASE
**[LAYAR]** Gerakkan Player (WASD) masuk ke depan `CowardEnemy`, masih di luar attack range. Tekan **Tab** sampai fokus ke CowardEnemy dan **zoom (Z)** sedikit.

**Narasi:**
> "Sekarang player masuk ke jarak pandang dan FOV enemy. `CanSeePlayer` menjadi Success, tetapi player masih di luar attack range, sehingga **Chase Sequence** yang berjalan dengan status Running. Current Action berubah menjadi **CHASE**, kucing berlari mengejar, dan player otomatis ikut berlari karena sedang dikejar."

### 03:40 – 04:00 · Demo Field of View
**[LAYAR]** Tekan **R**. Gerakkan Player ke **belakang** CowardEnemy saat enemy sedang patrol (dekat, tapi di luar FOV 90°).

**Narasi:**
> "Untuk menguji Field of View, player berdiri dekat tapi **di belakang** enemy. Karena berada di luar FOV 90 derajat, enemy tetap **PATROL** — padahal jaraknya dekat. Begitu player pindah ke depan, enemy langsung **CHASE**."

### 04:00 – 04:20 · Demo Obstacle / Line of Sight
**[LAYAR]** Saat dikejar, larikan Player ke balik tembok/pilar sampai garis pandang terhalang. Tunjuk Current Action berubah ke **SEARCH** lalu **PATROL**.

**Narasi:**
> "Sekarang player bersembunyi di balik tembok. Raycast mengenai obstacle sehingga player **tidak terlihat** walaupun masih dalam vision range. Enemy berpindah ke **SEARCH** — mendatangi posisi terakhir player terlihat, melihat sekeliling — lalu kembali **PATROL** karena tidak menemukan player."

### 04:20 – 04:50 · Demo ATTACK & COOLDOWN
**[LAYAR]** Biarkan enemy mendekat sampai attack range. Tampilkan Current Action **ATTACK**, animasi cakaran, health bar Player turun, dan **Console** (`attacks Player! Damage = 10`) dengan timestamp.

**Narasi:**
> "Saat player masuk attack range, **Attack Sequence** berhasil: player terlihat dan cukup dekat. Current Action menjadi **ATTACK** dan health player berkurang 10. Perhatikan Console: serangan **tidak terjadi setiap frame**, tetapi berjeda sesuai cooldown — 2 detik untuk Coward dan 0,8 detik untuk Aggressive. Ini fungsi **CooldownDecorator**: selama cooldown, node mengembalikan Running sehingga branch Attack tetap aktif tanpa menyerang terus-menerus. Selanjutnya *[Nama 3]* akan membahas Flee dan Utility AI."

---

## 🎙️ BAGIAN 3 — Orang 3 *(04:50 – 07:15)*
**Topik: Flee, Eksperimen Utility AI, Perbandingan, Bonus, Penutup**

### 04:50 – 05:25 · Demo FLEE
**[LAYAR]** Masih Play. Pilih `CowardEnemy` di Hierarchy → di Inspector, klik kanan komponen **EnemyBTController** → **Test Damage 25** dua kali (HP 100 → 50). Tampilkan Current Action **FLEE** dan enemy berlari ke bunker hijau (SafePoint).

**Narasi:**
> "Untuk menguji Flee, kami memberi damage ke CowardEnemy dengan menu **Test Damage 25**. Setelah health turun menjadi 50 — di bawah threshold Coward yaitu 70 — kondisi `IsHealthLow` Success dan **Flee Sequence** yang berada paling atas langsung aktif. Enemy berlari ke **SafePoint**, walaupun player masih terlihat dan dekat."

### 05:25 – 05:45 · Mengapa Flee di Atas Attack
**[LAYAR]** Ulangi pada `AggressiveEnemy`: Test Damage 25 tiga kali (HP 25) → masih ATTACK; sekali lagi (HP 0, ≤ 15) → FLEE.

**Narasi:**
> "Pada AggressiveEnemy, threshold-nya hanya 15, jadi di HP 25 ia **tetap menyerang**. Ini menunjukkan perilaku juga ditentukan oleh **parameter tuning**. Flee ditempatkan paling atas karena health kritis adalah kondisi darurat: Selector berhenti di Flee yang Running, sehingga Attack, Chase, dan Patrol tidak dievaluasi."

### 05:45 – 06:30 · Eksperimen Utility-Based AI
**[LAYAR]** Stop Play. Di `CowardEnemy`: uncheck `EnemyBTController`, centang `EnemyUtilityController`. Play. Tunjuk panel debugger berisi skor attack/chase/flee/patrol yang berubah real-time. Tampilkan slide perhitungan di bawah.

**Narasi:**
> "Sebagai eksperimen, kami mengganti controller ke **Utility AI**. Di sini enemy tidak memakai urutan prioritas, tetapi menghitung **skor 0 sampai 1** untuk setiap aksi dan memilih skor tertinggi.
> Rumusnya: **Attack** = terlihat × dalam jangkauan × (0,6 + 0,4 × kedekatan); **Chase** = 0,5 sampai 0,8 semakin jauh player; **Flee** = (1 − health%) × 1,2 × ancaman; dan **Patrol** = 0,1 sebagai aksi default."

**[LAYAR — slide contoh perhitungan]**

| Kondisi | Attack | Chase | Flee | Patrol | Utility memilih | BT memilih (threshold 30) |
|---|---|---|---|---|---|---|
| HP 100, terlihat, jarak 1 | **0.80** | 0.10 | 0.00 | 0.10 | ATTACK | ATTACK |
| HP 25, terlihat, jarak 1 | 0.80 | 0.10 | **0.90** | 0.10 | FLEE | FLEE |
| HP 40, **tidak terlihat** | 0.00 | 0.00 | **0.36** | 0.10 | FLEE | PATROL |

> "Pada kasus ketiga terlihat perbedaannya: Behavior Tree hanya melihat **ya/tidak** — health 40 belum di bawah threshold, jadi Patrol. Sedangkan Utility AI berubah **bertahap**: semakin terluka, keinginan kabur semakin besar. Untuk mencegah enemy berganti aksi terlalu cepat, kami menambahkan **action commitment** dan **hysteresis**."

### 06:30 – 06:50 · Perbandingan FSM, BT, Utility AI
**[LAYAR]** Slide tabel perbandingan.

| | FSM | Behavior Tree | Utility AI |
|---|---|---|---|
| Dasar keputusan | State + transition | Prioritas urutan Selector | Skor tertinggi |
| Menambah perilaku | Banyak transition baru | Tambah 1 branch | Tambah 1 fungsi skor |
| Perubahan perilaku | Tegas | Tegas (ya/tidak) | Bertahap |

**Narasi:**
> "Ringkasnya, **FSM** berpindah state melalui transition eksplisit, sehingga makin rumit saat perilaku bertambah. **Behavior Tree** memilih berdasarkan **prioritas** dan modular. **Utility AI** memilih berdasarkan **skor tertinggi** sehingga perilakunya lebih halus dan adaptif."

### 06:50 – 07:05 · Fitur Pengembangan (Bonus)
**[LAYAR]** Montase cepat: zoom close-up kucing (Z/Tab), health bar, panel BT Debugger (F1), dua kucing berbeda warna, animasi Walk/Run/Attack.

**Narasi:**
> "Sebagai pengembangan, kami juga menambahkan **Search Last Seen Position**, **Health Bar**, **Animator** untuk player dan enemy, **Personality** Aggressive dan Coward, **On-screen BT Debugger**, versi **hybrid Behavior Tree + Utility AI**, serta kamera zoom in-game."

### 07:05 – 07:15 · Penutup
**[LAYAR]** Tampilan arena penuh (R) atau slide penutup dengan nama anggota.

**Narasi:**
> "Demikian demo Praktikum 6 dari kelompok kami. Behavior Tree membuat keputusan NPC lebih terstruktur dan mudah dikembangkan, sedangkan Utility AI memberi keputusan yang lebih fleksibel. Terima kasih."

---

## 📋 Checklist Wajib Tertangkap di Video

- [ ] Identitas kelompok (nama + NRP seluruh anggota)
- [ ] Struktur & kode Behavior Tree (BTNode, Selector, Sequence, Condition, Action, Cooldown)
- [ ] **PATROL** saat player tidak terlihat
- [ ] **CHASE** saat player terlihat
- [ ] **ATTACK** saat player dalam attack range
- [ ] **Cooldown** — jeda serangan terlihat di Console
- [ ] **Perception / FOV** — player di belakang enemy tidak terlihat
- [ ] **Obstacle Line of Sight** — sembunyi di balik tembok
- [ ] **FLEE** saat health di bawah threshold
- [ ] **Current Action** terlihat (BT Debugger / Inspector)
- [ ] Eksperimen Utility AI (skor & perbandingan)

## 💡 Tips Rekaman

- Rekam **per bagian** (3 klip), lalu gabungkan — lebih mudah mengulang jika salah.
- Saat menunjuk sesuatu, **jeda 1–2 detik** agar penonton sempat melihat.
- Jika enemy sudah menyerang terlalu cepat sebelum adegan siap, tekan **Stop → Play** untuk reset posisi.
- Untuk adegan Flee, **jangan lupa kembalikan** controller ke `EnemyBTController` setelah segmen Utility AI selesai (atau rekam segmen Utility paling akhir, seperti urutan di script ini).
