# 💬 HaHaTalk (WPF 기반 실시간 메신저 솔루션)

> **"5년의 솔루션 구축 노하우를 투영한 고신뢰성 실시간 데이터 통신 플랫폼"**

---

## 📌 Project Origin & Attribution
본 프로젝트는 **'까불이코더'**의 오픈소스 프로젝트를 기반으로 시작되었으며, **원작자의 동의를 얻어** 기능을 대폭 확장 및 고도화했습니다. 기존 베이스 코드를 분석하여 **.NET 8 환경으로 현대화**하였으며, 특히 **실시간 SignalR 허브 서버 구축 및 MSSQL 데이터 직접 연동**을 주도하여 엔터프라이즈급 솔루션으로 재구축했습니다.

---

## 🚀 프로젝트 개요
**HaHaTalk**은 C# .NET 환경에서 MVVM 패턴을 기반으로 설계된 엔터프라이즈급 실시간 채팅 솔루션입니다. 5년간의 MES/ERP 개발 경험을 바탕으로, 데이터의 정밀한 흐름 제어와 시스템 안정성을 최우선으로 설계되었습니다. 특히 의료 및 제조 현장과 같이 24시간 중단 없는 운영이 필요한 환경을 가정하여 최적의 성능을 구현했습니다.

---

## 🛠 Tech Stack
* **Language**: C# (.NET 8)
* **Framework**: WPF (Windows Presentation Foundation)
* **Real-time**: **ASP.NET Core SignalR** (실시간 양방향 통신 허브 설계)
* **Pattern**: MVVM (CommunityToolkit.Mvvm)
* **Database**: MSSQL (Entity Framework Core 기반 데이터 매핑)
* **Async**: TPL (Task Parallel Library) 기반 비동기 로직
* **Infrastructure**: Windows Server, MSSQL DB Engine

---

## 🔍 트러블슈팅 및 성능 최적화 (Troubleshooting)

### 1. SignalR 실시간 통신 안정성 확보 및 세션 매핑
* **Issue**: 소켓 연결 시점과 로그인 세션의 불일치로 인해 메시지 전송 시 간헐적 오류 및 접속 상태 동기화 실패 발생.
* **Solution**: 
  * 서버 허브(Hub) 내부의 **연결/해제(OnConnected/Disconnected) 이벤트**를 핸들링하여 유저 ID와 커넥션 ID를 1:N으로 매핑하는 로직 구현.
  * 네트워크 순단 시 자동 재연결을 시도하는 `WithAutomaticReconnect` 정책을 적용하여 서비스 연속성 확보.

### 2. 비동기 처리를 통한 UI 응답성(Freezing) 개선
* **Issue**: 대규모 채팅 목록 로드 및 파일 전송 시 메인 UI 스레드 점유로 인한 화면 멈춤 현상 발생.
* **Solution**: 
  * `async/await` 및 `Task.Run`을 활용하여 DB 트랜잭션 및 I/O 작업을 백그라운드 스레드로 분리.
  * `BindingOperations.EnableCollectionSynchronization`을 적용하여 멀티 스레드 환경에서도 UI 컬렉션을 안전하게 업데이트함으로써 사용자 경험(UX) 개선.

### 3. 데이터베이스 연결 및 쿼리 예외 처리 최적화
* **Issue**: 프로젝트 연결 초기 단계에서 DB 서버 인증 및 데이터 타입 불일치로 인한 런타임 오류 발생.
* **Solution**:
  * 모든 DB 통신 구간에 **에러 로그 및 예외 처리(Try-Catch)**를 배치하여 병목 지점을 확인하고 직접 디버깅하여 해결.
  * 불필요한 전체 조회를 지양하고 필요한 컬럼만 선택적으로 가져오는 방식으로 데이터 로딩 속도 최적화.

---

## 📅 개발 히스토리 (Monthly Log)

### **2026-02: 프로젝트 착수 및 실시간 통신 코어 엔진 구축**
* **Core**: 원작 프로젝트 분석 및 .NET 8 마이그레이션 수행. **SignalR Hub 서버** 구축 및 메시지 브로드캐스팅 엔진 개발.
* **Architecture**: MVVM 패턴 기반의 프로젝트 구조 설계 및 기초 UI 스캐폴딩.

### **2026-03: 데이터베이스 설계 및 비즈니스 로직 연동**
* **Data**: MSSQL 기반 Entity Framework Core 연동 및 유저/채팅방 정보 스키마 설계.
* **Integration**: **로그인 세션 기반의 유저별 맞춤형 채팅 목록 로드 로직 직접 구현 및 디버깅.**

### **2026-04: 멀티미디어 기능 강화 및 시스템 최종 최적화**
* **Multimedia**: 이미지 및 일반 파일 첨부 기능 개발 및 비동기 전송 로직 구현.
* **Refactoring**: 가독성 향상을 위한 코드 리팩토링 및 인터페이스 기반의 예외 처리 강화 후 최종 완료.

---

## ✨ 핵심 기능
* **실시간 소켓 채팅**: SignalR을 활용한 초저지연 1:1 및 그룹 메시징 시스템.
* **실시간 접속 상태 감지**: 유저의 온/오프라인 상태 및 읽지 않은 메시지 카운트 실시간 갱신.
* **비동기 파일 시스템**: 이미지 프리뷰 지원 및 중단 없는 파일 업로드/다운로드 경험 제공.
* **직접 제어된 백엔드**: 직접 디버깅을 거쳐 안정성을 확보한 MSSQL 연동 및 데이터 처리 로직.

---

## 🏗 아키텍처 (MVVM Pattern)

View와 ViewModel의 철저한 분리를 통해 UI 변경에 유연하게 대응하며, 비즈니스 로직의 독립성을 확보했습니다. 특히 **인터페이스(Interface)**를 활용한 예외 처리로 시스템의 견고함을 더했습니다.
