using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommonLib.Models;
using HAHATalk.Stores;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Data;
using HAHATalk.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace HAHATalk.ViewModels
{

    [ObservableObject]
    public partial class ChatRoomViewModel
    {
        private readonly IChatService _chatService; // DB 접근용 저장소 
        private readonly UserStore _userStore;
        private ISignalRService _signalRService; // 2026.04.17 추가 

        // 2026.03.31 
        [ObservableProperty]
        private string _roomId;    // DB조회를 위한 키 (내 이메일)  
        [ObservableProperty]
        private string _targetId; // 상태방 이메일 
        // 채팅 상대방 정보 
        [ObservableProperty]
        private string _targetName; 
        [ObservableProperty]
        private string _targetProfile;

        // 입력창 테스트 
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
        private string _inputMessage;

        // 메시지 목록 (실시간 반영) 
        public ObservableCollection<ChatMessage> Messages { get; set; } = new();

        // 생성자 : 누구와의 채팅방인지 정보를 받음 
        public ChatRoomViewModel(
            string roomId,
            string targetId,
            string targetName,
            IChatService chatService,
            UserStore userStore,
            ISignalRService signalRService,
            string targetProfile = ""
            )
        {
            RoomId = roomId;
            _targetId = targetId;
            _targetName = targetName;
            _targetProfile = targetProfile;
            _chatService = chatService;
            _userStore = userStore;
            _signalRService = signalRService;

            // 초기 로드시 과거 내역 가져오기 
            //LoadInitialMessages();

            // 컬렉션 동기화 설정 (다른 스레드에서 Add 해도 에러 안나게 ) 
            BindingOperations.EnableCollectionSynchronization(Messages, new object());

            // 2026.04.17 실시간 메세지 수신 이벤트 등록 
            _signalRService.MessageReceived += OnMessageReceived;

            // ChatHub에서 JoinRoom 호출) 
            Task.Run(async () => await _signalRService.JoinRoom(RoomId));

            // 2026.03.31 
            // 비동기로 초기 메세지 로드 (Fire and Forget)
            Task.Run(async () => await LoadMessagesFromDb());
            //_ = LoadMessagesFromDb();
        }

        private async Task LoadMessagesFromDb()
        {
            try
            {
                // 1. Repository를 통해 DB에서 해당 방의 메시지 리스트를 가져옴
                // 예: SELECT * FROM ChatMessage WHERE RoomId = @roomId ORDER BY SendTime ASC
                var dbMessages = await _chatService.GetChatHistoryAsync(RoomId);

                // 2. UI 스레드에서 컬렉션 업데이트
                App.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Clear();
                    if(dbMessages != null)
                    {
                        foreach(var msg in dbMessages)
                        {
                            // DB에서 데이터를 가져올 때 
                            // 내 이메일과 발신자 이메일을 비교해서 IsMine 세팅 
                            msg.IsMine = (msg.SenderId == _userStore.CurrentUserId);
                            
                            // 2024.04.20 내가 보낸 게 아니라면 상대방 이름을 꽃아줌 
                            if(!msg.IsMine)
                            {
                                msg.SenderName = TargetName;
                            }

                            Messages.Add(msg);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"메시지 로드 실패: {ex.Message}");
            }
        }


        // 메세지 전송 커맨드 - 비동기(Async) 대응 
        [RelayCommand(CanExecute = nameof(CanSend))]
        private async Task SendMessage()
        {
            if(string.IsNullOrWhiteSpace(InputMessage))
                return;

            // 가상의 전송 로직 (나중에 SignalR/API 연동 부분) 
            var myMsg = new ChatMessage
            {
                RoomId = RoomId,
                SenderId = _userStore.CurrentUserId,
                SenderName = _userStore.CurrentUserNickname, 
                Message = InputMessage, 
                SendTime = DateTime.Now,
                IsMine = true, 
                IsRead = false,     // DB 컬럼 대응 
                MessageType = 0     // 기본 텍스트 타입 
            };

            // UI에 즉시 추가 
            Messages.Add(myMsg);

            string currentInput = InputMessage;

            // 입력창 비우기 
            InputMessage = string.Empty;

            // 서버 전송 및 DB 저장 로직 수행 
            try
            {
                // 메세지 저장 요청 
                bool saveSuccess = await _chatService.SaveMessageAsync(myMsg);

                // 2) 내 목록 & 상대방 목록 업데이트 
                // targetId는 roomId를 생성할 때 상대방 이메일 활용 

                if (saveSuccess)
                {
                    // 서버에서 생성 또는 업데이트할지 판단 
                    // 채팅모곩 업데이트 (상대방 / 내 목록 최신화) 
                    bool listUpdated = await _chatService.UpdateChatListAsync(
                                        myMsg,
                                        TargetId,
                                        TargetName,
                                        _userStore.CurrentUserId,
                                        _userStore.CurrentUserNickname
                                        );

                    // 2026.04.14 
                    // 처음 채팅이라면 , 메인화면의 채팅 목록을 새로고침하려는 
                    // 이벤트를 발생시켜야 함 
                    if (listUpdated)
                    {
                        // 처음 대화라면 목록 새로고침 알림 
                        WeakReferenceMessenger.Default.Send(new RefreshChatListMessage());

                       
                    }

                    await _signalRService.SendMessageAsync(RoomId, TargetId, currentInput);
                }
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"전송 에러 : {ex.Message}");
            }

        }

        // 메세지를 보낼 수 있는지 여부 (빈칸이거나 null이면 false) 
        private bool CanSend() => !string.IsNullOrWhiteSpace(InputMessage);


        // 2026.04.06 추가 
        [RelayCommand]
        private async Task SelectFile()
        {
            // 파일 탐색기 객체 생성 
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                // 필터 설정
                // 필요에 따라서 이미지, 문서, pdf등 별로 확장자 생성 
                Filter = "모든 파일 (*.*)|*.*|이미지 파일 (*.jpg;*.png)|*.jpg;*.png|문서 파일 (*.pdf;*.docx)|*.pdf;*.docx",
                // 여러개 (2개이상 선택)은 해제 
                Multiselect = false
            };

            // 탐색기 호출 및 결과 확인 
            if (openFileDialog.ShowDialog() == true)
            {
                // 원본 위치
                string sourceFile = openFileDialog.FileName;

                // 원본 이름 
                string originFileName = openFileDialog.SafeFileName;

                // 저장할 파일 경로 가져오기 
                string uploadFolder = GetUploadPath();

                // 고유 파일 명 생성 (중복방지) 
                string extension = Path.GetExtension(originFileName);
                string uniqueName = $"{Guid.NewGuid()}{extension}";
                // 저장되는 파일명 : 폴더 경로 + 고유파일명 
                string destinationFile = Path.Combine(uploadFolder, uniqueName);

                try
                {
                    // 비동기로 파일 복사 (UI 프리징 방지) 
                    await Task.Run(() => File.Copy(sourceFile, destinationFile, true));

                    // DB 저장용 메서드 호출 (MessageType 1) 
                    // DB에는 unique(실제 경로) 및 originName(UI 표시용)을 모두 보내야 
                    await SendFileMessageAsync(originFileName, destinationFile);
                }
                catch(Exception ex)
                {
                    // 에러처리 
                    System.Diagnostics.Debug.WriteLine($"Failed to Uplaod: {ex.Message}");
                }
            }
        }


        ///<summary>
        /// 실행파일경로 기준 Uploads 폴더를 반환 없으면 폴더 생성 
        ///</summary> 
        private string GetUploadPath()
        {
            // AppDomain.CurrentDomain.BaseDirectory : .exe이 있는 폴더경로 
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        private async Task SendFileMessageAsync(string originFileName, string destinationFile)
        {
            // Model 생성 
            var chatMessage = new ChatMessage
            {
                RoomId = RoomId, 
                SenderId = _userStore.CurrentUserId, 
                SenderName = _userStore.CurrentUserNickname, 
                Message = originFileName, 
                MessageType = 1, 
                FilePath = destinationFile, 
                FileName = originFileName,
                SendTime = DateTime.Now,
                IsMine = true,
                IsRead = false
            };

            // DB 저장 (Repository 호출) 
            bool isSaved = await _chatService.SaveMessageAsync(chatMessage);

            if(isSaved == true)
            {
                // 내 목록 & 상태 목록 '마지막 메세지'업데이트 
                // 파일 전송 시에도 목록 정보 갱신 (첫 메세지가 파일일 수도 있음!!!) 
                await _chatService.UpdateChatListAsync(
                    chatMessage, 
                    TargetId, 
                    TargetName, 
                    _userStore.CurrentUserId, 
                    _userStore.CurrentUserNickname                    
                    );

                // UI 콜렉션에 추가 (화면에 말풍선 띄우기) 
                App.Current.Dispatcher.Invoke(() =>Messages.Add(chatMessage));

                System.Diagnostics.Debug.WriteLine($"[파일 전송 성공] 저장경로 : {destinationFile}");
            }
        }

        // 2026.04.08 Add 
 
        [RelayCommand]
        public async Task OpenImage(string path)
        {
            if(string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                // 파일이 없거나 경로가 잘못된 경우 
                System.Diagnostics.Debug.WriteLine("파일이 존재하지 않습니다.");
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    // 시스템 기본 뷰어로 파일 진행 
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path)
                    {
                        UseShellExecute = true
                    });
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"파일 열기 실패: {ex.Message}");
                }
            });
        }

        private void OnMessageReceived(string senderEmail, string message)
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR 수신 체크] 발신자: {senderEmail}, 메시지: {message}");

            // 내가 보낸 게 아니고 현재 열려 있는 이 채팅방의 메세지??
            if (senderEmail == TargetId)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Add(new ChatMessage
                    {
                        RoomId = RoomId,
                        SenderId = senderEmail,
                        SenderName = TargetName,
                        Message = message,
                        SendTime = DateTime.Now,
                        IsMine = false
                    });

                });
            }
        }

    }
}
