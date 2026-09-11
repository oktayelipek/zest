# Zest proje değerlendirmesi — 10 Eylül 2026

## Sonuç

Zest'in kullanılabilir bir simülasyon temeli ve belirgin bir görsel yönü var. Ancak ana oyun, bu sistemleri uçtan uca çalışan bir işletme döngüsünde henüz birleştirmiyor. Mevcut durum: test edilmiş domain bileşenleri + etkileşimli görsel prototip. Tekrar oynanabilir bir yönetim oyunu olarak kabul etmek için erken.

En büyük darboğaz yeni asset veya yeni mekanik eksikliği değil, mevcut mekaniklerin aynı oyun oturumunda birbirine bağlanmaması. Baştan yazma gerektiren bir kanıt bulmadım. Önce entegrasyon ve doğruluk, ardından oyun dengesi ve görsel genişleme öneriyorum.

## Kapsam ve doğrulama

README, docs altındaki mimari/UI/sanat dokümanları, kaynak ve runtime sanat README'leri; domain, config, oyun sunumu, runner ve doğrulama araçları incelendi. Yerel dosyalar esas alındı; harici GDD/Trello taranmadı. Mevcut `docs/artifacts/demo-recording-contact.png` görseli incelendi; bu inceleme sırasında yeni etkileşimli oynanış veya çoklu çözünürlük testi yapılmadı.

- `dotnet test Zest.sln --nologo`: 57 domain + 6 config testi geçti; atlanan/başarısız test yok.
- `dotnet build Zest.sln -c Release --nologo`: başarılı, 0 uyarı, 0 hata.
- SimRunner çalıştı. Ürettiği satış elle gönderilen örnek bir muhasebe işlemidir; gerçek müşteri-satış entegrasyon testi değildir.
- Godot ana sahne `--headless --path game --quit-after 60` ile hatasız başladı. Bu yalnız başlangıç kontrolüdür.
- 13 `validate-*.ps1` betiğinin 11'i geçti. `validate-ui04-product-status.ps1`, `"CLASSIC PAUSED"` beklentisinde; `validate-world-first-ui.ps1`, eski `WAITING` metni beklentisinde başarısız oldu. Bunlar doğrudan oynanış arızası kanıtı değil, metne bağımlı kontrollerin kodla eskimesi.
- Yerel Git deposunun henüz commit'i yok; mevcut proje dosyaları untracked. Başka yerde yedek bulunup bulunmadığı bilinmiyor.

## Öncelikli bulgular

### P0 — Ana oyun gerçek satış döngüsünü çalıştırmıyor

`game/scripts/Main.cs` içindeki `_Ready`, boş GameState oluşturuyor ve üç kaynak kaydediyor. Config yüklemiyor, tarif kitabı ve başlangıç envanteri kurmuyor, açılış nakdi vermiyor. Oyun projesi yalnız Domain'e referans veriyor. `_Process`, saati ve müdahale sürelerini ilerletiyor; dünya fırsatı, müşteri seçimi, stok rezervasyonu, sipariş üretimi/ilerletilmesi ve satış muhasebesini çalıştırmıyor.

`DayCommandProcessor.StartLive` yalnız faz değiştiriyor; `Advance` yalnız zamanı ve müdahaleleri ilerletiyor. Dolayısıyla aynı komut sınırını kullanmak bugün aynı tam simülasyonu çalıştırmak anlamına gelmiyor. ART-12 study modundaki beş kişilik kuyruk da `StartInLiveStudy ? 5 : snapshot.QueueCount` ile atanıyor.

**Yapılacak:** Godot ve SimRunner'ın ortak kullandığı bir oturum/simülasyon yürütücüsü kur. Config → başlangıç durumu → fırsat → seçim → rezervasyon → iş sırası → servis → stok tüketimi → ledger zincirini sabit adımlarda çalıştır.

**Kabul:** Sabit seed'li bir senaryoda gerçek müşteri gelir, sipariş verir, ürün teslim edilir; stok azalır, satış ve maliyet yalnız bir kez kaydedilir. UI ve runner aynı komutlarla aynı sonucu verir.

### P0 — Görünen stok ve fiyat gerçek durumu temsil etmiyor

Main'in stand paneli, dünya tabelası ve inventory ekranı ile `OperationalDiagnostics`, stoku `PlannedBatchSize + RemainingServings` şeklinde hesaplıyor. Açılış planı tüketilen stok değildir; satış bağlansa bile bu değer kendiliğinden azalmaz. `DayCommandProcessor.Snapshot` ise ham malzeme miktarlarını topluyor; bu da satılabilir bardak sayısı değildir.

Fiyat müdahalesi `CurrentPrices` içine yazılırken stand paneli `PlannedPriceMinor` göstermeye devam ediyor. Ürün durdurulunca dünya nesnesi `SoldOut` durumuna giriyor; elde stok olmasıyla satışın geçici durması birbirine karışıyor.

**Kabul:** Tek bir ürün görünümü fiyat, satılabilir miktar, rezervasyon, tazelik ve satış durumunu üretmeli. Satış, fiyat değişikliği, teslimat ve bozulmadan sonra panel, tabela ve notebook aynı gerçeği göstermeli. Açık panelin ETA ve durumu da zaman ilerlerken yenilenmeli.

### P0 — Gün tamamlanmıyor ve ertesi güne geçilemiyor

`ShowReport` rapor kartlarıyla bitiyor. `DayCommands` içinde yeni gün komutu ve rapordan sabaha dönüş yok. Saat 18:00'de yalnız ekrandaki zaman üst sınıra sabitleniyor; otomatik kapanış yok. Saat çalışmaya devam edebiliyor. Kapanış, bekleyen siparişler ve gün sonu stok/maliyetleri için tamamlayıcı süreç yürütmüyor.

`SimulationClock` 86.400 adımda DayIndex artırırken UI 36.000 birimi on saatlik işletme günü sayıyor. Her adım 100 ms gerçek zamanda bir SimTime birimi artırdığı için 1× açık gün yaklaşık 60 gerçek dakika, 4× yaklaşık 15 dakika sürüyor. Bu zorunlu olarak hata değil; adım/saniye/gün anlamı ve hedef seans süresi açıkça kararlaştırılmalı.

**Kabul:** Kapanış bir kez gerçekleşir; kalan işler için açık politika uygulanır; rapor oluşturulur; nakit ve kalıcı durum korunarak ikinci sabaha geçilir. Üç gün kesintisiz oynanır.

### P0 — Yerel çalışma için geri dönüş noktası yok

Git deposunda hiç commit bulunmuyor. Bu, sanat denemeleri ve kod değişikliklerinde güvenilir karşılaştırma/geri alma olanağını azaltıyor.

**Yapılacak:** Cache ve sırlar hariç başlangıç sürümü, uygun büyük dosya politikası ve uzak yedek oluştur. İnceleme kapsamında commit veya uzak depoya yükleme yapılmadı.

### P1 — Müşteri gösterimi kimliğe ve sonuca bağlı değil

`ParkWorldView`, kuyruk sayısı değişince aktörleri yeniden oluşturuyor; kalıcı CustomerId yerine sıra indeksi kullanıyor. Sayı azalınca ilk müşteriye satış sonrası ayrılma pozu veriyor; terk etme ve servis ayrımı yok. Müşteri kartı herkese sabit patient/likely to buy metni gösteriyor. Pin sonucu başarısız olsa bile Main takip bildirimi verebiliyor.

**Kabul:** Aktörler CustomerId ile korunur; servis alan ve vazgeçen farklı olaylarla ayrılır; seçili müşteri kimliği kaymaz; gözlem verisi gerçek veya açıkça bilinmiyor olur.

### P1 — Yürüyüş FPS'e bağımlı; pause dünyayı durdurmuyor

`HdCustomerActor._Process`, her karede `Position + direction * 36 * delta` değerini yuvarlayıp yeniden Position'a yazıyor. 60 FPS'te eksen hareketi 0,6 pikselden 1'e yuvarlanır; 120 FPS'te 0,3 pikselden 0'a düşebilir. Kesirli ilerleme kaybolduğu için yüksek FPS'te yürüyüş durabilir. Bu koddan çıkarımdır; farklı FPS'lerde canlı ölçüm yapılmadı.

Rota aktörleri Godot delta'sıyla, ekonomi ise ayrı SimulationClock ile çalışıyor. Pause/2×/4× seçimi müşteri hareket zamanına iletilmiyor.

**Kabul:** Hassas konum ayrı tutulur, yalnız çizim konumu yuvarlanır. 30/60/120 FPS'te eşit mesafe; pause ve hız seçimleri için tutarlı davranış. Kozmetik rüzgârın bağımsız kalması ayrı bir tasarım kararı olabilir.

### P1 — Hazırlık, ikmal ve muhasebe sözleşmesi eksik

`PrepareExtraBatch` anında hazır porsiyon ekliyor; ham malzeme tüketimi ve gerçek hazırlık işi yaratmıyor. Süresi dolan porsiyonlar sayaç/trace'e yazılıyor, bu yolda Waste ledger işlemi yok. İkmal hazır batch eklerken normal rezervasyon sistemi ham lotlara dayanıyor. Bunların aynı siparişte nasıl birleşeceği netleştirilmeli.

`EconomyService.PostServedOrder`, gelirle birlikte COGS'u nakitten düşürüyor; ikmal de ayrıca nakit gideri. Ücretli ikmalin ürünü servis edilirken maliyeti ikinci kez nakitten düşürmemesi için tedarik/COGS politikası belirlenmeli. Henüz entegre olmayan akışta bu, çift sayım riski; gerçekleşmiş satış hatası olarak sunulmamalı.

### P1 — Yeşil kontroller fazla dar bir güven veriyor

Testler değerli fakat bileşen ağırlıklı. UI/sanat betiklerinin çoğu dosya, metin ve boyut varlığı kontrol ediyor. Yürüyüşün her karede yuvarlanması gibi sorunlu bir satırın bulunmasını bile başarı koşulu sayan kontrol var. Tam gün, gerçek satış, açık panel güncellemesi ve aktör-simülasyon eşleşmesi doğrulanmıyor.

**Eklenecek anlamlı testler:** ortak runner ile tam gün; stok tükenmesi; teslimat; vazgeçme sonrası rezervasyon bırakma; fiyatın sonraki müşterilere etkisi; aynı seed/komutun farklı frame aralıklarında eşit sonucu; ikinci gün geçişi.

### P1 — Güncel görsel sözleşme tek bir yerde yok

TECHART-01 Scale=1 ve native kaynak isterken aktif stand sahnesi yaklaşık 0,1107, satıcı 0,041, müşteri 0,035 ölçek kullanıyor. ART-04 eski native atlası üretim sonucu olarak tanımlıyor; aktif Base ise AI katmanları, upgrade'ler eski atlas. ART-10 içinde hem 32×48 hem 16×24 karakter tarifi var. AI katman README'sinde hem ana oyuna bağlı değil hem aktif ifadeleri bulunuyor. Son asset planı bu çelişkilerin çoğunu zaten doğru teşhis etmiş, ancak otorite olan karar metni güncellenmemiş.

AI katmanları için önceki kullanıcı onayı dokümanlarda kayıtlı; çözüm bunları izinsiz veya geçersiz saymak değil. Aktif üretim yöntemini dürüstçe kaydetmek, eski kararları superseded işaretlemek ve teknik entegrasyonla görsel kabulü ayırmak.

Mevcut contact sheet'te sarı tente, ahşap gövde ve park kimliği okunuyor. Stand yanında çok sayıda durum levhası birikiyor; küçük HUD/panel yazıları gerçek çıktı boyutlarında ayrıca incelenmeli. Contact sheet üzerinden nihai okunurluk onayı verilmedi.

### P2 — Uzun dönem oyun ve ürünleşme eksikleri

- Persistence projesi yalnız proje dosyası; save/load/migration uygulaması yok.
- ProgressionState yalnız boş unlock kümesi; upgrade satın alma/ilerleme döngüsü yok.
- Tek tarif Classic. Berry UI'da sürekli sold out olarak bulunuyor; kullanılabilir içerik değil.
- Hava, itibar, harita ve müşteri gözlemlerinin önemli kısmı sabit metin. Config'teki dünya çeşitliliği ana oyuna ulaşmıyor.
- Rapor yorumları sabit; oyuncunun kararından çıkardığı ders kişiselleşmiyor.
- README'deki çalıştırma betiği belirli kullanıcının Downloads içindeki Godot yoluna bağlı. Taşınabilir başlatma, export/dağıtım ve otomatik CI akışı eksik.
- Ses, ayarlar, erişilebilirlik ve dil desteği için tamamlanmış ürün akışı görünmüyor. Bunlar ilk entegrasyon düzeltmesinin önüne geçmemeli.

## Kurtarma sırası

1. **Mevcut durumu sabitle:** Git başlangıcı/yedek; tek güncel durum belgesi; aktif sanat kararını yaz. Yeni asset ailesi üretimini geçici olarak durdur.
2. **Tek gerçek satış:** Ortak runtime; config, nakit, stok ve tarif başlangıcı; müşteri → sipariş → servis → ledger. Tek park ve Classic yeterli.
3. **Bir gerçek gün:** Satılabilir stok görünümü, fiyat etkisi, kuyruk terk etme, kapanış ve sonuç raporu. Entegrasyon testlerini geçir.
4. **Üç günlük karar döngüsü:** Ertesi gün geçişi, kaydet/yükle, bir anlamlı upgrade. Oyuncu dün aldığı karara göre bugün farklı seçim yapabilsin.
5. **Görsel ve his iyileştirmesi:** Kimlik koruyan müşteriler, doğru animasyon olayları, FPS/pause düzeltmeleri, okunurluk ve kısa ses geri bildirimleri. Sonra çevreyi ve ürünleri genişlet.

İlk başarı ölçütü dosya, kart veya PNG sayısı olmamalı: oyuncu bir karar verir, dünyada sonucunu görür, raporda nedenini anlar ve ertesi gün farklı bir seçim yapabilir. Projenin mevcut teknik birikimini oyuna dönüştürecek eşik budur.
