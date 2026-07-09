# 🏦 BankaCuzdan - Modern Çekirdek Bankacılık Sistemi (Core Banking)


<img width="1919" height="873" alt="image" src="https://github.com/user-attachments/assets/cd9bb7a4-9714-49f1-b3b8-1cd847655260" />


**BankaCuzdan**, kurumsal seviyede güvenlik, çift taraflı muhasebe (double-entry ledger) ve gerçek zamanlı işlem yeteneklerine sahip, Full-Stack bir FinTech / Çekirdek Bankacılık simülasyonudur. Sadece bir para transfer uygulaması değil; arkasında risk motorları, kredi onay iş akışları ve sıkı güvenlik protokolleri barındıran eksiksiz bir finansal ekosistemdir.

---

## ✨ Öne Çıkan Özellikler

### 🛡️ Kurumsal Güvenlik ve Şifreleme
* **2FA TOTP Entegrasyonu:** Kullanıcı hesapları Google Authenticator uyumlu Zamana Dayalı Tek Kullanımlık Şifre (TOTP) altyapısı ile korunmaktadır.
* **AES-256 Şifreli Sanal Kartlar:** Veritabanında sanal kredi kartı verileri Luhn algoritmasıyla üretilir ve AES-256 ile askeri düzeyde şifrelenerek saklanır.
* **Stealth Loader (FOUC Koruması):** Admin paneli gibi yetki gerektiren sayfalarda, yetkisiz kullanıcılar saniyelik bile olsa arayüzü göremez (Flash of Unauthenticated Content zafiyeti kapalıdır).
* **Güvenli Şifre Yönetimi:** Kullanıcı şifreleri BCrypt ile hashlenir.

### 💰 Çift Taraflı Muhasebe (Double-Entry Ledger)
Sistemde para asla "yok olmaz" veya "havadan var edilmez". 
* **Ön Ödemeli Sanal Kart Havuzu:** Ana hesaptan sanal karta para yüklendiğinde, arka planda bankanın Merkez Havuzu kullanılarak çift bacaklı (double-entry) transfer kaydı oluşturulur.
* **BSMV ve Komisyon Kesintileri:** Para transferlerinde yasal vergiler (BSMV) ana bakiyeden düşülerek doğrudan banka gelir havuzuna aktarılır.

### ⚡ Gerçek Zamanlı Deneyim (Real-Time UI)
* **SignalR (WebSockets) Bildirimleri:** Bir kullanıcıya para gönderildiğinde, alıcının sayfasını yenilemesine gerek kalmadan saniyesinde ekranına bildirim düşer ve bakiyesi anlık olarak güncellenir. 
* **Glassmorphism Tasarım:** Koyu tema (Dark Mode) ve cam efekti ağırlıklı, modern ve akıcı bir kullanıcı deneyimi (Next.js & Tailwind CSS).

### 📈 Kredi Risk Motoru ve Onay İş Akışı (Workflow)
* **Dinamik Faiz ve Vade Primi:** Kredi türüne ve vadesine göre bankanın riskini hesaplayan dinamik faiz motoru.
* **Katı Ödeme Pencereleri:** Erken ödemelere sınır getirilmiş ve gecikmeye düşen krediler için günlük **Gecikme Faizi** işletilen gerçekçi bir sistem kurgulanmıştır.
* **Admin Onay Kuyruğu:** Krediler "Beklemede" statüsüne alınır ve sadece yetkili Admin'ler onayladığında likidite tahsisi yapılır.

### 📄 Yasal Raporlama ve Paylaşım
* **PDF Dekont ve Ekstre:** **QuestPDF** altyapısı kullanılarak, kullanıcılara transfer fişleri ve hesap ekstreleri basılı fatura kalitesinde PDF olarak sunulur.
* **Web Share API:** Mobil cihazlarda dekontlar WhatsApp, Mail vb. platformlara tek tıkla Native (yerel) olarak paylaşılabilir.

---

## 🛠️ Teknoloji Yığını (Tech Stack)

### Backend (Core API) - [Repo Linki](https://github.com/MrcDprm/BankCoreApi)
* **Framework:** C# / .NET 8 (ASP.NET Core Web API)
* **ORM:** Entity Framework Core
* **Real-time:** SignalR (WebSockets)
* **Security:** JWT Authentication, BCrypt, AES-256
* **Reporting:** QuestPDF

### Frontend (Client) - [Repo Linki](https://github.com/MrcDprm/banka-frontend)
* **Framework:** Next.js (React 18, App Router)
* **Styling:** Tailwind CSS (Glassmorphism & Koyu Tema)
* **Network:** Axios
* **Real-time:** `@microsoft/signalr`
* **UX/UI:** QR Code (qrcode.react), Web Share API

---

## 🚀 Kurulum ve Çalıştırma (Lokal Geliştirme)

Sistem iki ayrı repo olarak tasarlanmıştır. Lokalinizde çalıştırmak için her iki projeyi de ayağa kaldırmanız gerekmektedir.

### 1. Backend'i Ayağa Kaldırma (`BankCoreApi`)
```bash
git clone [https://github.com/MrcDprm/BankCoreApi.git](https://github.com/MrcDprm/BankCoreApi.git)
cd BankCoreApi

# appsettings.Local.json.example dosyasının adını appsettings.Local.json yapın
# İçine kendi veritabanı bilgilerinizi ve JWT gizli anahtarınızı girin.
dotnet restore
dotnet ef database update
dotnet run
````

API varsayılan olarak http://localhost:5217 portunda çalışacaktır.

### 2. Frontend'i Ayağa Kaldırma (banka-frontend)
```bash
git clone [https://github.com/MrcDprm/banka-frontend.git](https://github.com/MrcDprm/banka-frontend.git)
cd banka-frontend

npm install

# .env.example dosyasını .env.local olarak kopyalayın
# NEXT_PUBLIC_API_BASE_URL=http://localhost:5217 değerini kontrol edin.

npm run dev
````
Arayüze http://localhost:3000 adresinden erişebilirsiniz.

## 📸 Ekran Görüntüleri

<table>
  <tr>
    <td width="50%">
      <h3 align="center">📊 Dashboard (Ana Panel)</h3>
      <p align="center">
        <img width="1919" height="869" alt="image" src="https://github.com/user-attachments/assets/75a7c4bf-4c23-4f25-9a8a-fa342e6f2a9e" />
      </p>
      <p align="center">Kullanıcıların bakiyelerini, sanal kartlarını ve işlemlerini yönettiği cam efekti (glassmorphism) tasarımlı ana arayüz.</p>
    </td>
    <td width="50%">
      <h3 align="center">📄 PDF Dekont & Ekstre</h3>
      <p align="center">
        <img width="1132" height="917" alt="image" src="https://github.com/user-attachments/assets/e7ce1032-e300-4b45-87ca-1bf411bf0f25" />
      </p>
      <p align="center">QuestPDF ile oluşturulmuş, kurumsal kalitede basılabilir işlem fişleri ve Web Share API destekli paylaşım ekranı.</p>
    </td>
  </tr>
  <tr>
    <td width="50%">
      <h3 align="center">🛡️ Admin Risk Yönetimi</h3>
      <p align="center">
        <img width="1919" height="867" alt="image" src="https://github.com/user-attachments/assets/4844e01c-2775-42a7-9a7c-f564095f96cc" />
      </p>
      <p align="center">Banka kasasının durumunu izleyen ve kredi başvurularının onay/red işlemlerinin yapıldığı izole yönetim paneli.</p>
    </td>
    <td width="50%">
      <h3 align="center">⚡ Gerçek Zamanlı Bildirimler</h3>
      <p align="center">
       <img width="1919" height="867" alt="image" src="https://github.com/user-attachments/assets/daf146f2-92ae-41c6-a8b6-3817795ddeb8" />
      </p>
      <p align="center">SignalR altyapısı sayesinde sayfa yenilemeye gerek kalmadan ekrana düşen anlık transfer bildirimleri.</p>
    </td>
  </tr>
</table>

## 💡 Mimari Notlar
* **Muhasebe altyapısında "Tek bacaklı" (Single-leg) finansal işlem yoktur. Her transfer çift bacaklı bir Transaction bloğu içinde gerçekleştirilir.**

* **React Strict Mode geliştirme ortamında SignalR el sıkışma hatalarını önlemek için asenkron try-catch yapıları kullanılmıştır.**

* **Public alanlarda şifrelerin sızmaması için .env ve appsettings.Local.json üzerinden gizlilik sağlanmıştır.**
