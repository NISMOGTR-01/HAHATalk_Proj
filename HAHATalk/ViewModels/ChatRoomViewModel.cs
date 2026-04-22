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
using HAHATalk.Messages;

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

            // 컬렉션 동기화 설정 (다른 스레드에서 Add 해도 에러 안나게 ) 
            BindingOperations.EnableCollectionSynchronization(Messages, new object());

            // 2026.04.17 실시간 메세지 수신 이벤트 등록 
            _signalRService.MessageReceived += OnMessageReceived;

            // ChatHub에서 JoinRoom 호출) 
            Task.Run(async () => await _signalRService.JoinRoom(RoomId));

            // 2026.03.31 
            // 비동기로 초기 메세지 로드 (Fire and Forget)
            Task.Run(async () => await LoadMessagesFromDb());

            // 2024.04.22 서버에서 SignalR로 메세지가 오면 Messenger가 이 신호를 낚아 챈다 
            WeakReferenceMessenger.Default.Register<NewMessageReceivedMessage>(this, (r, m) =>
            {
                // 내 방 번호와 일치하는 메시지인지 확인
                if (m.Message.RoomId == this.RoomId)
                {
                    if (m.Message.SenderId != _userStore.CurrentUserEmail)
                    {
                        // UI 스레드에서 리스트에 추가
                        App.Current.Dispatcher.Invoke(async () =>
                        {
                            // Dto 데이터를 Model 객체로 변환 
                            var newMessage = new ChatMessage
                            {
                                RoomId = m.Message.RoomId,
                                SenderId = m.Message.SenderId,
                                SenderName = m.Message.SenderName,
                                Message = m.Message.Message,
                                SendTime = m.Message.SendTime,
                            };

                            Messages.Add(newMessage);

                            // [읽음 처리 추가] 수신 즉시 서버와 상대방에게 알림
                            await _chatService.MarkAsReadAsync(RoomId, _userStore.CurrentUserId);
                            await _signalRService.SendReadReceiptAsync(RoomId, _targetId);
                        });
                    }
                }
            });

            // 2026.04.22 상대방이 내가 보낸 메시지를 읽었을 때 처리
            WeakReferenceMessenger.Default.Register<MessagesReadMessage>(this, (r, m) =>
            {
                if (m.RoomId == this.RoomId)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var msg in Messages)
                        {
                            msg.IsRead = true; // 실시간으로 '1' 제거
                        }
                    });
                }
            });
        }

        private async Task LoadMessagesFromDb()
        {
            try
            {
                // 1. Repository를 통해 DB에서 해당 방의 메시지 리스트를 가져옴
                var dbMessages = await _chatService.GetChatHistoryAsync(RoomId);

                // 2. UI 스레드에서 컬렉션 업데이트
                App.Current.Dispatcher.Invoke(async () =>
                {
                    Messages.Clear();
                    if (dbMessages != null)
                    {
                        foreach (var msg in dbMessages)
                        {
                            // DB에서 데이터를 가져올 때 
                            // 내 이메일과 발신자 이메일을 비교해서 IsMine 세팅 
                            msg.IsMine = (msg.SenderId == _userStore.CurrentUserId);

                            // 2024.04.20 내가 보낸 게 아니라면 상대방 이름을 꽃아줌 
                            if (!msg.IsMine)
                            {
                                msg.SenderName = TargetName;
                            }

                            Messages.Add(msg);
                        }
                    }

                    // [읽음 처리 추가] 히스토리 로드 후 서버에 읽음 보고 및 상대방 알림
                    await _chatService.MarkAsReadAsync(RoomId, _userStore.CurrentUserId);
                    await _signalRService.SendReadReceiptAsync(RoomId, _targetId);
                    WeakReferenceMessenger.Default.Send(new RefreshChatListMessage());
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
            if (string.IsNullOrWhiteSpace(InputMessage))
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
                if (saveSuccess)
                {
                    // 채팅목록 업데이트 (상대방 / 내 목록 최신화) 
                    bool listUpdated = await _chatService.UpdateChatListAsync(
                                        myMsg,
                                        TargetId,
                                        TargetName,
                                        _userStore.CurrentUserId,
                                        _userStore.CurrentUserNickname
                                        );

                    // 2026.04.14 처음 채팅이라면 목록 새로고침 알림 
                    if (listUpdated)
                    {
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
                Filter = "모든 파일 (*.*)|*.*|이미지 파일 (*.jpg;*.png)|*.jpg;*.png|문서 파일 (*.pdf;*.docx)|*.pdf;*.docx",
                Multiselect = false
            };

            // 탐색기 호출 및 결과 확인 
            if (openFileDialog.ShowDialog() == true)
            {
                string sourceFile = openFileDialog.FileName;
                string originFileName = openFileDialog.SafeFileName;
                string uploadFolder = GetUploadPath();
                string extension = Path.GetExtension(originFileName);
                string uniqueName = $"{Guid.NewGuid()}{extension}";
                string destinationFile = Path.Combine(uploadFolder, uniqueName);

                try
                {
                    // 비동기로 파일 복사 (UI 프리징 방지) 
                    await Task.Run(() => File.Copy(sourceFile, destinationFile, true));
                    await SendFileMessageAsync(originFileName, destinationFile);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to Uplaod: {ex.Message}");
                }
            }
        }

        private string GetUploadPath()
        {
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

            if (isSaved == true)
            {
                // 내 목록 & 상태 목록 업데이트 
                await _chatService.UpdateChatListAsync(
                    chatMessage,
                    TargetId,
                    TargetName,
                    _userStore.CurrentUserId,
                    _userStore.CurrentUserNickname
                    );

                // UI 콜렉션에 추가 
                App.Current.Dispatcher.Invoke(() => Messages.Add(chatMessage));
            }
        }

        // 2026.04.08 Add 
        [RelayCommand]
        public async Task OpenImage(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                return;

            await Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path)
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"파일 열기 실패: {ex.Message}");
                }
            });
        }

        private void OnMessageReceived(string senderEmail, string message)
        {
            // 내가 보낸 게 아니고 현재 열려 있는 이 채팅방의 메세지??
            if (senderEmail == TargetId)
            {
                App.Current.Dispatcher.Invoke(async () =>
                {
                    // 2026.04.22 읽음 처리 통합
                    try
                    {
                        await _chatService.MarkAsReadAsync(RoomId, _userStore.CurrentUserId);
                        await _signalRService.SendReadReceiptAsync(RoomId, _targetId);
                        WeakReferenceMessenger.Default.Send(new RefreshChatListMessage());
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"읽음 처리 중 에러: {ex.Message}");
                    }
                });
            }
        }
    }
}