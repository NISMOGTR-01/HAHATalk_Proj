# 💬 HaHaTalk (WPF 기반 실시간 메신저 솔루션)

> **"5년의 솔루션 구축 노하우를 투영한 고신뢰성 실시간 데이터 통신 플랫폼"**

---

## 📌 Project Origin & Attribution
본 프로젝트는 **'까불이코더'**의 오픈소스 프로젝트를 기반으로 시작되었으며, 원작자의 동의를 얻어 기능을 대폭 확장 및 고도화했습니다. 기존 베이스 코드를 분석하여 **.NET 10 환경으로 현대화**하였으며, 특히 **실시간 SignalR 허브 서버 구축 및 MSSQL 데이터 직접 연동**을 주도하여 엔터프라이즈급 솔루션으로 재구축했습니다.

---

## 🚀 프로젝트 개요
**HaHaTalk**은 C# .NET 환경에서 MVVM 패턴을 기반으로 설계된 실시간 채팅 솔루션입니다. 5년간의 MES/ERP 개발 경험을 바탕으로, 데이터의 정밀한 흐름 제어와 시스템 안정성을 최우선으로 설계되었습니다. 특히 의료 및 제조 현장과 같이 24시간 중단 없는 운영이 필요한 환경을 가정하여 최적의 성능을 구현했습니다.

---

## 🛠 Tech Stack
* **Language**: C# (.NET 10)
* **Framework**: WPF (Windows Presentation Foundation)
* **Real-time**: **ASP.NET Core SignalR** (실시간 양방향 통신 허브 설계)
* **Pattern**: MVVM (CommunityToolkit.Mvvm)
* **Database**: MSSQL (Entity Framework Core 기반 데이터 매핑)
* **State Management**: Singleton Store Pattern
* **Infrastructure**: Windows Server, MSSQL DB Engine

---

## 🔍 트러블슈팅 및 성능 최적화 (Troubleshooting)

### 1. 데이터 무결성(Data Integrity) 확보 및 Dirty Read 방지
* **Issue**: 프로필 편집 시 참조 타입(Reference Type) 객체 공유로 인해 '확인'을 누르기 전 메인 UI 데이터가 미리 변경되는 현상 발생.
* **Solution**: 편집 시 원본 대신 **임시 객체(Clone)**를 전달하고, `DialogResult`가 확정된 시점에만 `Messenger`를 통해 데이터를 커밋하는 로직을 구현하여 데이터 오염을 완벽 차단.

### 2. 환경 설정 분리를 통한 보안 및 유지보수성 강화
* **Issue**: API 주소 및 서버 설정값이 소스 코드 내에 하드코딩되어 보안 취약점 및 배포 환경 전환의 어려움 발생.
* **Solution**: `ConfigurationBuilder`를 도입하여 **appsettings.json** 기반으로 설정을 분리. `.gitignore`를 통해 민감 정보를 관리하고, DI(Dependency Injection)를 통해 설정값을 주입받는 구조로 개선.

### 3. WPF 이미지 캐싱 및 실시간 갱신 문제 해결
* **Issue**: 서버의 프로필 이미지가 업데이트되었음에도 클라이언트에서 이전 이미지를 계속 보여주는 현상.
* **Solution**: 이미지 경로 뒤에 **Query String(Ticks)**을 추가하여 브라우저/WPF 캐시를 우회하고, 실시간으로 변경된 리소스를 로드하도록 처리.

---

## 📅 개발 히스토리 (Monthly Log)

### **2026-02: 프로젝트 착수 및 현대화**
* **Core**: 원작 프로젝트 분석 및 .NET 10 마이그레이션 수행.
* **Architecture**: DI 컨테이너 구축 및 기초 UI 스캐폴딩.

### **2026-03: 실시간 통신 코어 및 DB 엔진 구축**
* **Real-time**: **ASP.NET Core SignalR Hub 서버 직접 구축** 및 메시지 브로드캐스팅 엔진 개발.
* **Data**: MSSQL 기반 EF Core 연동 및 유저/채팅 데이터 스키마 설계.

### **2026-04: 보안 강화 및 시스템 고도화**
* **Infrastructure**: API 주소 및 민감 정보 JSON 분리 관리 및 보안 관리 체계 강화.
* **Reliability**: 프로필 편집 로직의 데이터 무결성 확보 및 **WeakReferenceMessenger** 기반 이벤트 아키텍처 완성.
* **Refactoring**: 인터페이스 기반 예외 처리 강화 및 전역 상태 저장소(UserStore) 최적화.

---

## ✨ 핵심 기능
* **실시간 소켓 채팅**: SignalR을 활용한 초저지연 1:1 및 그룹 메시징 시스템.
* **전역 상태 관리**: `UserStore`를 통한 로그인 세션 및 프로필 데이터의 실시간 동기화.
* **보안 최적화**: 민감 정보 분리 및 환경별 설정(Local/Dev/Prod) 대응 구조.
* **비동기 처리**: TPL 기반 비동기 로직으로 UI Freezing 현상 근본적 해결.

---

## 🏗 아키텍처 (MVVM Pattern)

View와 ViewModel의 철저한 분리를 통해 UI 변경에 유연하게 대응하며, **WeakReferenceMessenger**와 **UserStore**의 조합으로 대규모 데이터 동기화 시에도 낮은 결합도를 유지하도록 설계되었습니다.
