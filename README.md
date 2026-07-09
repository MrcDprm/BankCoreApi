[🇬🇧 English Version](#english-version)

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


<a id="english-version"></a>
## 🇬🇧 English Version
# 🏦 BankaCuzdan - Modern Core Banking System

<img width="1919" height="873" alt="image" src="https://github.com/user-attachments/assets/cd9bb7a4-9714-49f1-b3b8-1cd847655260" />

**BankaCuzdan** is a full-stack FinTech / Core Banking simulation featuring enterprise-level security, a double-entry ledger, and real-time transaction capabilities. It is not merely a money transfer app; it is a comprehensive financial ecosystem backed by dynamic risk engines, credit approval workflows, and strict security protocols.

---

## ✨ Highlighted Features

### 🛡️ Enterprise Security & Encryption
* **2FA TOTP Integration:** User accounts are protected by a Google Authenticator-compatible Time-based One-Time Password (TOTP) infrastructure.
* **AES-256 Encrypted Virtual Cards:** Virtual credit card data is generated using the Luhn algorithm and stored in the database with military-grade AES-256 encryption.
* **Stealth Loader (FOUC Protection):** On authorization-required pages (like the Admin panel), unauthorized users cannot see the interface even for a millisecond (Flash of Unauthenticated Content vulnerability is closed).
* **Secure Password Management:** User passwords are cryptographically hashed using BCrypt.

### 💰 Double-Entry Ledger
In this system, money never "disappears" or is "created out of thin air". 
* **Prepaid Virtual Card Pool:** When funds are loaded onto a virtual card from the main account, a double-entry transfer record is created in the background using the bank's Central Pool.
* **BSMV and Commission Deductions:** Legal taxes (BSMV) on money transfers are deducted from the main balance and transferred directly to the bank's revenue pool.

### ⚡ Real-Time Experience (Real-Time UI)
* **SignalR (WebSockets) Notifications:** When money is sent to a user, a notification drops on their screen instantly and the balance is updated in real-time without needing to refresh the page. 
* **Glassmorphism Design:** A modern and fluid user experience heavily featuring Dark Mode and glass effects (Next.js & Tailwind CSS).

### 📈 Credit Risk Engine and Approval Workflow
* **Dynamic Interest and Maturity Premium:** A dynamic interest engine that calculates the bank's risk based on the credit type and maturity.
* **Strict Payment Windows:** A realistic system is built where early payments are restricted and a daily **Late Fee** is applied for overdue credits.
* **Admin Approval Queue:** Credits are put into a "Pending" status and liquidity allocation is only made when authorized Admins approve.

### 📄 Legal Reporting and Sharing
* **PDF Receipt and Statement:** Using the **QuestPDF** infrastructure, transfer receipts and account statements are presented to users as printed invoice-quality PDFs.
* **Web Share API:** On mobile devices, receipts can be shared Natively with a single click to platforms like WhatsApp, Mail, etc.

---

## 🛠️ Tech Stack

### Backend (Core API) - [Repo Link](https://github.com/MrcDprm/BankCoreApi)
* **Framework:** C# / .NET 8 (ASP.NET Core Web API)
* **ORM:** Entity Framework Core
* **Real-time:** SignalR (WebSockets)
* **Security:** JWT Authentication, BCrypt, AES-256
* **Reporting:** QuestPDF

### Frontend (Client) - [Repo Link](https://github.com/MrcDprm/banka-frontend)
* **Framework:** Next.js (React 18, App Router)
* **Styling:** Tailwind CSS (Glassmorphism & Dark Theme)
* **Network:** Axios
* **Real-time:** `@microsoft/signalr`
* **UX/UI:** QR Code (qrcode.react), Web Share API

---

## 🚀 Installation and Setup (Local Development)

The system is designed as two separate repos. You need to spin up both projects to run it locally.

### 1. Running the Backend (`BankCoreApi`)
```bash
git clone [https://github.com/MrcDprm/BankCoreApi.git](https://github.com/MrcDprm/BankCoreApi.git)
cd BankCoreApi

# Rename the appsettings.Local.json.example file to appsettings.Local.json
# Enter your own database information and JWT secret key inside it.
dotnet restore
dotnet ef database update
dotnet run
````

The API will run on the http://localhost:5217 port by default.

### 2. Running the Frontend (banka-frontend)
```bash
git clone [https://github.com/MrcDprm/banka-frontend.git](https://github.com/MrcDprm/banka-frontend.git)
cd banka-frontend

npm install

# Copy the .env.example file as .env.local
# Check the NEXT_PUBLIC_API_BASE_URL=http://localhost:5217 value.

npm run dev
````
You can access the interface from http://localhost:3000.

## 📸 Screenshots

<table>
  <tr>
    <td width="50%">
      <h3 align="center">📊 Dashboard (Main Panel)</h3>
      <p align="center">
        <img width="1919" height="869" alt="image" src="https://github.com/user-attachments/assets/75a7c4bf-4c23-4f25-9a8a-fa342e6f2a9e" />
      </p>
      <p align="center">The glassmorphism-designed main interface where users manage their balances, virtual cards, and transactions.</p>
    </td>
    <td width="50%">
      <h3 align="center">📄 PDF Receipt & Statement</h3>
      <p align="center">
        <img width="1132" height="917" alt="image" src="https://github.com/user-attachments/assets/e7ce1032-e300-4b45-87ca-1bf411bf0f25" />
      </p>
      <p align="center">Enterprise-quality printable transaction receipts generated with QuestPDF, featuring Web Share API integration.</p>
    </td>
  </tr>
  <tr>
    <td width="50%">
      <h3 align="center">🛡️ Admin Risk Management</h3>
      <p align="center">
        <img width="1919" height="867" alt="image" src="https://github.com/user-attachments/assets/4844e01c-2775-42a7-9a7c-f564095f96cc" />
      </p>
      <p align="center">An isolated management panel to monitor the bank's central pool and handle the credit approval/rejection queue.</p>
    </td>
    <td width="50%">
      <h3 align="center">⚡ Real-Time Notifications</h3>
      <p align="center">
       <img width="1919" height="867" alt="image" src="https://github.com/user-attachments/assets/daf146f2-92ae-41c6-a8b6-3817795ddeb8" />
      </p>
      <p align="center">Instant transfer notifications pushed directly to the screen via SignalR infrastructure without requiring a page refresh.</p>
    </td>
  </tr>
</table>

## 💡 Architectural Notes
* **There are no "Single-leg" financial transactions in the accounting infrastructure. Every transfer is executed within a double-entry Transaction block.**

* **Asynchronous try-catch structures were used to prevent SignalR handshake errors in the React Strict Mode development environment.**

* **Privacy is ensured via .env and appsettings.Local.json so that passwords do not leak in public areas.**
