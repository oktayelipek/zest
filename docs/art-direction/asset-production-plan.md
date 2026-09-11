# Zest — Asset üretim planı

Tarih: 10 Eylül 2026. Durum: doküman/config incelemesine dayalı üretim önerisi; tek tek assetlere verilmiş görsel onay değildir.

## Kapsam ve kaynak güveni

Hedef Windows/Steam; 1080p UI baseline, 640×360 dünya ve 1440p/4K çıktı. Onaylı görsel yön `art-source/concepts/art04/visual-target-v01/zest-park-visual-target.png`. Kullanıcı AI destekli oyun katmanlarına ve yerel alfa temizliğine izin verdi. Assetler elle çizilmiş native pixel art olarak etiketlenmeyecek.

Bu plan yerel dokümanlar, config ve asset manifesti üzerinden hazırlandı. Tam bir harici GDD, sanat kaynak dosyaları veya yeni bir Trello kart açıklaması taraması yapılmadı. Trello eşleştirmeleri konuşmada son okunan kart başlıklarına dayanır; kartların bütün kabul şartlarını kapsadığı varsayılmamalı. Süre ve bütçe taahhüdü içermez.

Bir satır bir üretim paketi/asset ailesidir; PNG sayısı veya animasyon kare sayısı değildir. P0 ilk görsel olarak tutarlı oynanabilir sahne, P1 mevcut mekanikleri görünür kılan playable genişlemesi, P2 ileriki sistemler, P3 yayın materyalidir. Bunlar önerilen önceliklerdir.

## Önce çözülmesi gereken üretim kararları

| Çelişki / açık karar | Kanıt | Üretime etkisi / öneri |
| --- | --- | --- |
| Dükkân boyutu | ART-04: 170×136; benchmark: yaklaşık 256×192 kaynak; onaylı kare: saksılarla yaklaşık 300 dünya pikseli genişlik | Kaynak boyutu, dünya ayak izi ve kamera kadrajı ayrı alanlar olmalı. Mevcut 170 genişlik ile onaylı yakın kadraj aynı test sahnesinde karşılaştırılmadan tüm yükseltmeler çizilmemeli. |
| Gövde oranı | Yeni gövde 1536×1024; eski hedef 170×136 | Yeni görseli 170×136'ya esnetmek yasak. En-boy oranını koru; saydam padding ve gerçek ayak noktasını ölç. |
| Karakter boyutu | ART-10 içinde hem 32×48 hem 16×24; ART-12 runtime sheet 192×192/4×4 = 48×48 hücre | Hücre boyutu ile görünür beden ölçüsü aynı değildir. Satıcı/müşteri aynı karede ölçeklenmeli; tek hücre standardı bu testten sonra seçilmeli. |
| Palet | Eski manifest 24 RGBA girdisine kilitli; onaylı AI görünümü bu palete doğrulanmış değil | Eski prototip validator'ünü gevşetme. Yeni AI katmanları için ayrı katalog ve açık renk politikası tanımla; otomatik palet azaltmayı kalite çözümü sayma. |
| Runtime ölçek | TECHART fractional sprite scale yasak diyor; yeni gövde sahnesi HD kaynağı küçültüyor | Yeni yol istisna olarak kayıtlı. Kaynak→export→dünya ölçüsü akışı belirlenip render ADR güncellenmeli; şu an uyum tamamlanmış değil. |
| Tamamlanma durumu | Eski dokümanlarda shipping/production deniyor; görsel reset ve kullanıcı reddi var | Dosyanın bulunması tamamlanma sayılmaz. Kaynak, teknik doğrulama, entegrasyon ve görsel kabul ayrı durumlar olmalı. |

İlk karar teslimi: aynı satıcı ve müşteriyle küçük sahnede iki dükkân kadrajı, kuyruk ve açık UI paneli. Ekranı kaplama, karakter oranı, tıklama alanı ve yazı okunurluğu birlikte değerlendirilecek. Tüm art üretiminden önce bu ölçü kilitlenmeli.

## Mevcut durum

| Varlık | Gerçek durum | Yapılacak |
| --- | --- | --- |
| Onaylı park kompozisyonu | Görsel yön onaylı; tek düz görsel | Stil referansı, bağımsız oyun nesneleri yerine kullanılmamalı. |
| Yeni satıcısız gövde | RGBA temizliği ve Godot yüklemesi doğrulandı; ana oyuna bağlı değil | Gövde/ekipman/bitki ayrımı ve gerçek kadraj QA. |
| P0 satıcı + gövde entegrasyonu | Base stand ana oyuna bağlı; idle/prepare/pour/handoff key-pose seti order durumuna bağlandı; 1080p QA kareleri alındı | Ara geçiş ve secondary-motion kareleri yok; yakın kamerada dünya etiketleri kırpılıyor; upgrade'ler eski atlası kullanıyor. |
| Eski dört karelik dükkân atlası | Ana oyunda programmer art | Yerine geçecek set onaylandığında runtime referansını değiştir. |
| Eski park, ağaç, bank, lamba, çiçeklik | Runtime mevcut; onaylı yeni stile göre final kabul yok | Yeniden kullanımı karşılaştırmalı sahne incelemesine bağla. |
| Eski müşteri yürüyüş sheet'i / rüzgâr çalısı | Teknik animasyon örnekleri mevcut | Hareket ve stil açısından incele; referans olarak kullanılabilir. |
| UI skin ve glyph'ler | Kod tabanlı bileşenler mevcut | Gereksiz yere bütün panelleri PNG olarak yeniden üretme. |

## A — İlk oynanabilir sahne: P0

| ID | Asset paketi | Önerilen teslim / davranış | Bağımlılık ve kart |
| --- | --- | --- | --- |
| Z-ST-01 | Dükkân arka gövde | Direkler, arka iç bölüm; dekoratif sabit tabela ayrı kaynak katmanında | Ölçek kararı; ART-04 |
| Z-ST-02 | Tente ve flama | Sabit bağlantı noktası; hafif rüzgâr için 4 kare başlangıç önerisi | Gövde pivotu; ART-04 |
| Z-ST-03 | Ön tezgâh ve kaplama | Satıcıyı doğru örten ön katman; gölge ve ayak izi | Z-ST-01; ART-04 |
| Z-ST-04 | Temel ekipman | Dispenser, bardak yığını, limon kasası, şeker kabı, su kabı; ayrı düzenlenebilir parçalar | classic tarifi, basic-stand; ART-04/E05 |
| Z-ST-05 | Menü / durum tabelası | Boş yüzey; metin koddan. Açık, kapalı, paused, sold-out, rush durumları | UI-04/FX-01; paused ile sold-out aynı anlamda gösterilmemeli |
| Z-CH-01 | Satıcı | Sarı şapka/yeşil önlük; idle, hazırlama, doldurma, uzatma animasyonları | Z-ST-03 örtüşmesi; ART-04/ART-07 |
| Z-CH-02 | Bir temel müşteri | student segmenti; dört yön iki-kare yürüyüş, idle/bekleme, ürünü alma ve satış sonrası yan taşıma pozu | ART-05/ART-07; leave asset hazır, served-order hook/turn ve çoklu varyantlar sonraki geçiş |
| Z-PR-01 | Classic bardak | Tezgâh üstü ve elde kullanılan aynı tasarım; dolu/boş durum | E05/ART-04/ART-07 |
| Z-EN-01 | Sakin çimen ve yol | Çimen tabanı, az sayıda varyasyon; yol düz/kenar/iç-dış köşe/geçiş | ART-03; 16 birim yerleşim gridi, piksel hücre kararı ayrı |
| Z-EN-02 | Küçük çevre seti | Bir ağaç, bank, çalı, taş kümesi ve çiçeklik; gövde/taç ayrımı gereken yerlerde | ART-03; önce küçük test alanı |
| Z-FX-01 | Seçim ve temas | Ayak gölgesi, seçili müşteri işareti, hover/stand seçim sınırı | FX-01/UI-06; arazi üzerinde okunurluk |

P0 döngüsü: müşteri gelir → dükkânı fark eder → kuyruğa girer → satıcı hazırlar → bardak uzatılır → müşteri ayrılır. İlk sahnede tek müşteriyle başla; kabul kontrolünde sekiz kişilik kuyruğu da göster. Kalabalık durum dekor üretiminden önce okunmalı.

## B — Operasyon, varyasyon ve çevre: P1

| ID | Asset paketi | Önerilen teslim / tetikleyici | Kart / dayanak |
| --- | --- | --- | --- |
| Z-ST-06 | Better Counter | Mevcut gövdeye oturan yükseltilmiş ön tezgâh ve düzenli servis tepsileri | ART-04; upgrade görseli var, config'te ayrı tanım henüz yok |
| Z-ST-07 | Electric Juicer | Ayrı cihaz, idle/çalışıyor; kısa sıkma hareketi | ART-04; yeni config/mekanik eşleştirmesi gerekir |
| Z-ST-08 | Bigger Cooler | Kapalı/açık kapak, stok doluluğu; kasa yerleşimi | ART-04; ortak pivot |
| Z-ST-09 | Batch ve stok durumları | Hazır bardak tepsisi; dolu/az/boş gösterim; taze/bayat bilgi UI'da | E08-02/UI-04 |
| Z-CH-03 | Müşteri görünüş çeşitliliği | İlk öneri: temel rig üzerinde 3 görünüş; saç, kıyafet, çanta | ART-05; yeni segment icat etmez |
| Z-CH-04 | Davranış seti | Fark etme, düşünme, sabırsızlık, vazgeçme, memnun tepki | ART-07/UI-06; animasyon kayıp sebebini uydurmamalı |
| Z-CH-05 | Geçici yardımcı | Mevcut rig'den ayırt edilen kıyafet; geliş/çalışma/ayrılma | E08-03; yalnız gerçek varıştan sonra çalışır |
| Z-PR-02 | Acil ikmal | İkmal kasası ve teslim işareti; taşıyıcı karakter gerekirse mevcut rig | E08-03; ETA dolmadan stok dolu görünmez; araç üretimi şart değil |
| Z-EN-03 | Riverside su/kenar | Su döngüsü, kıyı geçişi, kıyı taşı, sazlık ve küçük iskele | ART-03; ilk sade park karesinden sonra |
| Z-EN-04 | Park genişleme parçaları | İp çit uç/köşe/orta, lamba ve tabela, ikinci ağaç/çalı varyasyonu | ART-03; aynı yoğunluk ve ışık |
| Z-EN-05 | Rüzgâr | Çalı/taç/flama için farklı fazlar, sabit kök ve ayak noktaları | ART-03/ART-08; bütün objeyi esnetme yerine lokal hareket |
| Z-FX-02 | İşlem geri bildirimi | Küçük dolum sıçraması, hazır bardak vurgusu, satın alma/onay işareti | FX-01/ART-11; nakit değişimi ledger olayından |
| Z-FX-03 | Gözlem işaretleri | Fiyat, ürün uyumu, bekleme, stok durumları için 4 ikon ailesi + nötr bilinmiyor | UI-06/E08-04; gözlemlenen kanıta göre |
| Z-WE-01 | Hava seti | Sunny/cloudy/rain ikonları; bulut gölgesi, yağmur parçacığı ve küçük sıçrama | ART-08; weather.json'daki üç durum |
| Z-WE-02 | Gün içi ışık | Sabah/öğle/kapanış renk ayarı; lamba parlaması gerekiyorsa ayrı katman | ART-08/UI-08; tüm sprite'ları üç kez üretme |

## C — UI ve yönetim yüzeyleri

| ID / öncelik | Paket | Gerekenler | Dayanak |
| --- | --- | --- | --- |
| Z-UI-01 / P0 | HUD temel ikonları | Saat, gün, üç hava, sıcaklık, nakit, itibar; mevcut glyph'leri değerlendir | UI-02 |
| Z-UI-02 / P0 | Kontrol durumları | Pause/play/hız, stand/customer/notebook/map; hover, selected, focus, disabled | UI-08/UI-11/UI-12; çerçeveler Godot theme/geometry |
| Z-UI-03 / P0 | Ürün/envanter | Classic, limon, şeker, su ikonları; miktar/tazelik/durum satırları | UI-04/E05; metin görsele gömülmez |
| Z-UI-04 / P1 | Müşteri gözlem | Düşünce balonu zemin/ucu, pin, bekleme ve ruh hâli işaretleri | UI-06/E08-04; zorunlu bilgi tooltip içinde saklanmaz |
| Z-UI-05 / P1 | Sabah ve gün sonu | Odak simgesi; gelir/maliyet/kayıp nedenleri ikonları; journal boş durum | UI-05/UI-09/E10-01/E10-02 |
| Z-UI-06 / P2 | Harita ve yerel bilgi | Park planı, stand/rota/ilgi işaretleri, bilinmeyen/bilinen durum | UI-07/E13-01; ikinci mahalle asseti henüz gerekmez |
| Z-UI-07 / P2 | Marka ve ilerleme | Brand Mirror işaretleri, kilit/açık upgrade görselleri | UI-10/E09-03/E10-03; dekoratif büyüme grafiği yerine gerçek bilgi |

UI ikonlarında başlangıç için 24/32 px nominal tasarım önerilir; kesin ölçü tema ve ekran içi QA ile seçilir. Dünya tabelaları kısa; yoğun metin native UI'da. Kontrast, klavye focus'u ve metin uzaması kontrolleri her panel paketine dahildir. Ayrı raster panel görseli, standart buton başına PNG veya her dil için yeni tabela üretimi planlanmaz.

## D — Sonraki içerik: P2

- Maya: ART-06/E09-02. Onaylı temel müşteri rig'inden ayırt edilebilir siluet/aksesuar, yüz ifadesi ve tanıma işareti. İlk pakete bağımsız tam karakter sistemi olarak eklenmez.
- Sadakat/geri dönüş: E09-01/E09-02. Tanınan müşteri işareti, selamlaşma ve rutin animasyon tekrar kullanımı.
- Berry ve diğer içecekler: UI dokümanında berry işareti var; config'te yalnız Classic bulunuyor. Tarif kabul edilene kadar yeni ürün sprite ailesi bekler.
- Pazarlama: E13-02. Pano/el ilanı/kampanya işaretleri; hangi kanal oynanışa girecekse ona göre seçilecek, hepsi peşinen üretilmeyecek.
- Save/load: E11. Kaydetme bildirimi, boş kayıt ve hata durumları mevcut UI skin'inden türetilir; özel dünya sprite'ı gerektirmez.
- Senaryo/test araçları: E13-03/E13-04. Yeni dekor gerektirmez; QA sahneleri, karşılaştırma görüntüleri ve kayıt düzeni gerekir.

## E — Ses ve yayın varlıkları

Ses için incelenen dokümanlarda ayrıntılı tasarım bulunmadı; aşağıdakiler önerilen ayrı backlog kapsamıdır.

- P1 ses denemesi: park ambiyansı, adım, dolum, bardak bırakma, servis, kısa UI onay/uyarı. Aynı sesi her kare veya hızlı sim adımında tetikleme; eşzamanlılık/cooldown sınırı.
- P2 ses: yağmur/rüzgâr katmanları, juicer çalışma döngüsü, sade müzik döngüsü. Müzik, efekt ve ambiyans için bağımsız ses kontrolleri.
- P3 yayın: Zest logo master, uygulama ikonu, gerçek gameplay ekran görüntüleri, kısa trailer görüntüleri ve Steam mağaza/kütüphane görselleri. Boyutlar üretim anında güncel Steamworks şartlarından doğrulanacak; bu plan güncel platform ölçüsü iddiası içermez. Konsept kare gameplay screenshot olarak sunulmaz.
- Her üçüncü taraf/AI varlığında kaynak, üretim aracı, kullanım koşulu kaydı; font ve ses dosyalarının lisansları da dahil.

## Animasyon sözleşmesi ve iş yükü

Başlangıç önerisi, henüz çizim siparişi değil:

| Aktör | Hareket | Başlangıç kare bütçesi | Not |
| --- | --- | --- | --- |
| Temel müşteri | Walk | 4 yön × 4 kare = 16 | S/W/E/N; asimetrik çanta varsa kör aynalama yapılmaz |
| Temel müşteri | Idle/wait | 4 yön × 2 kare = 8 | Sabit ayak pivotu |
| Temel müşteri | Receive/react | İlk etapta servis yönünde 4'er kare | Yönleri ihtiyaca göre genişlet |
| Satıcı | Idle | 2–4 | Omuz/başta ölçülü hareket |
| Satıcı | Prepare/pour/handoff | Hareket başına 4–6 | Ön tezgâhla örtüşme testi |
| Çalı/tente | Wind | 4 | Temas noktası sabit; farklı başlangıç fazı |
| Juicer/cooler | Active/open-close | 4–6 | Mekanik olaya bağlı; kapalı durum sürekli döngü değil |

Bu örnekte müşteri başlangıç seti 32 kare pozisyonudur (16+8+4+4). Üç tam görünüş tüm karelerde farklı çizilecekse 96 pozisyon kontrolü gerekir. Palette swap, aksesuar katmanı veya rig tekrar kullanımı emeği azaltabilir; üç PNG diye tahmin yapılmaz. Cell/pivot, action, yön, FPS, loop/one-shot ve event işaretleri katalogda saklanır. UI ve simülasyon, animasyonun bitişine göre para/stok değiştirmez; animasyon domain olayını gösterir. Pause/1×/2×/4× ve uzun frame testleri yapılır.

## Üretim sırası ve kabul kapıları

1. **Ölçek ve katalog:** güncel ölçü/palet çelişkilerini çöz; kaynak ve dünya boyutlarını ayır. Yeni AI katmanları için asset kayıt yapısı oluştur.
2. **Temel tezgâh:** Z-ST-01…05 + Z-CH-01 + Z-PR-01. Katmanlar yeniden birleşince onaylı görünümü korumalı; siyah/açık/yeşil zemin üzerinde alfa kontrolü.
3. **Bir müşteri ve küçük park:** Z-CH-02 + Z-EN-01/02 + Z-FX-01 + P0 UI. Geldi/satın aldı/ayrıldı döngüsü; bir ve sekiz müşteriyle derinlik/tıklama testi.
4. **Operasyon görünürlüğü:** üç upgrade, batch, yardımcı ve ikmal. Normal/paused/sold-out/rush ve kapasite durumları. Müşteri tepkileri.
5. **Riverside, hava ve gün:** çevre genişletme; okunurluk korunurken hareket/ışık ekle.
6. **Yönetim ve uzun dönem:** rapor/journal/regulars/harita/marka; sonra yayın materyali.

Her kapıda 1080p gameplay screenshot, yakın görünüm ve kısa hareket kaydı istenir. En az 720p/1080p/1440p/4K, pencere yeniden boyutlandırma, yavaş kamera hareketi, seçim ve UI paneli açık hâli test listesinde. Bunlar henüz tamamlanmış testler değildir. ART-09 tutarlılık kontrolü yalnız son güne bırakılmaz; her kapıda uygulanır.

## Asset teslim kaydı

Her varlık için: ID, amaç ve bağlı domain/config olayı; Trello kartı; öncelik; mevcut durum; referans ve kaynak yolu; düzenlenebilir katmanlar; export yolu; kaynak boyutu; dünya ayak izi; hücre/sheet düzeni; pivot/örtüşme maskesi; eylem/yön/FPS; palette yaklaşımı; import ayarları; lisans/provenance; görsel QA kanıtı; onay tarihi tutulmalı.

Durum akışı: planned → candidate → technical-pass → integrated → visual-approved. Yeniden çizim gerekiyorsa rework. Konseptin onayı bütün sprite'ların onayı sayılmaz. Yeni AI sprite'larını eski prototip üretim komutunun üzerine yazmamak için ayrı sürüm klasörü kullanılmalı. Tekrar üretilebilir alfa temizliği ve kaynak dosyaları korunmalı.

## İncelenen yerel kaynaklar

- `README.md`, `docs/architecture.md` — domain sınırları ve temel simülasyon döngüsü.
- `docs/ui/world-first-live-interface.md`, `docs/ui/ui-11-hybrid-pixel-components.md` — HUD, gözlem, inventory ve UI durumları.
- `docs/architecture/e08-03-pressure-interventions.md`, `docs/architecture/e08-04-active-diagnostics.md` — ikmal, yardımcı ve kanıta dayalı gözlem.
- `docs/art-direction/art-10-pixel-production-pipeline.md`, `art-12-first-playable-pixel-world.md`, `art-12-visual-benchmark-reset.md`, `art04-visual-recovery.md` — mevcut üretim yaklaşımı ve çelişkiler.
- `docs/architecture/techart-01-pixel-rendering.md` — rendering, ölçek ve animasyon sözleşmeleri.
- `art-source/pipeline.json`, `art-source/production/ai-layered-v01/README.md` — manifest ve son gövde adayının gerçek durumu.
- `config/customers/segments.json`, `config/products/{recipes,ingredients}.json`, `config/operations/{equipment,stations,staff}.json`, `config/world/{locations,weather}.json` — bugün tanımlı içerik.
