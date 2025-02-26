# ElasticsearchDemo

Bu proje, ASP.NET Core Web API kullanarak MSSQL ve Elasticsearch arasında veri senkronizasyonu sağlayan bir örnek uygulamadır.

## Özellikler

- ASP.NET Core 9.0 Web API
- MSSQL veritabanı entegrasyonu (Dapper ORM ile)
- Elasticsearch entegrasyonu (NEST kütüphanesi ile)
- Otomatik veri senkronizasyonu
- Swagger UI desteği
- Kapsamlı loglama
- Docker desteği ile Elasticsearch ve Kibana

## Gereksinimler

- .NET 9.0 SDK
- MSSQL Server
- Docker ve Docker Compose
- Elasticsearch 8.x (Docker üzerinde çalışacak)

## Kurulum

1. MSSQL Server'da veritabanını oluşturun:
   ```sql
   Database/CreateProductsTable.sql
   ```

2. Elasticsearch ve Kibana'yı Docker ile başlatın:
   ```bash
   docker-compose up -d
   ```

3. Elasticsearch'ün hazır olduğunu kontrol edin:
   ```bash
   curl http://localhost:9200
   ```

4. Kibana'ya erişin (opsiyonel):
   ```
   http://localhost:5601
   ```

5. `appsettings.json` dosyasındaki bağlantı bilgilerini güncelleyin:
   - DefaultConnection: MSSQL bağlantı bilgileriniz
   - ElasticsearchConnection: http://localhost:9200

6. Projeyi çalıştırın:
   ```bash
   dotnet run
   ```

7. Swagger UI'a erişin:
   ```
   https://localhost:5001/swagger
   ```

## Docker Compose Yapılandırması

Docker Compose ile aşağıdaki servisler başlatılır:

- Elasticsearch (port: 9200, 9300)
  - Single-node cluster
  - X-Pack security devre dışı
  - 512MB heap size
  - Persistent volume ile veri saklama

- Kibana (port: 5601)
  - Elasticsearch yönetim arayüzü
  - Elasticsearch'e otomatik bağlanır

Docker servisleri durdurmak için:
```bash
docker-compose down
```

Tüm verileri silmek için:
```bash
docker-compose down -v
```

## API Endpoints

- POST /api/product - Yeni ürün ekle
- PUT /api/product/{id} - Ürün güncelle
- DELETE /api/product/{id} - Ürün sil
- GET /api/product/{id} - Ürün getir
- GET /api/product - Tüm ürünleri getir

## Veri Senkronizasyonu

- Uygulama başlatıldığında, mevcut tüm ürünler Elasticsearch'e senkronize edilir
- MSSQL'e yapılan her CRUD işlemi otomatik olarak Elasticsearch'e yansıtılır
- Elasticsearch'te sadece ProductId, ProductName, Price ve Category alanları saklanır

## Hata Yönetimi

- Tüm işlemler try-catch blokları ile korunmuştur
- Hatalar ILogger ile loglanır
- API'ler uygun HTTP durum kodları ile yanıt verir

## Lisans

MIT 