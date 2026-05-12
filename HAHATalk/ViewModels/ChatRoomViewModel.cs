using CommonLib.Enums;
using CommonLib.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HAHATalk.Messages;
using HAHATalk.Services;
using HAHATalk.Stores;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Packaging;
using System.Windows.Data;

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
        private string _roomId;    // DB조회를 위한 키 (내 이메일)  
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

        // 2026.04.22 
        [ObservableProperty]
        private bool _isWindowActive; // 창이 활성화 상태인지 체크 

        // 2026.04.29 
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ParticipantIcon))] // Count 변경 시 Icon도 갱신 알림
        private int _participantCount = 2; // 기본 2로 설정 

        public string ParticipantIcon => ParticipantCount > 2 ? "GroupIconPath" : "SingleUserIconPath";

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
            _chatService = chatService;
            _userStore = userStore;
            _signalRService = signalRService;

            // 기존의 복잡한 if-else 대신 ChatService의 메서드를 호출.
            var fullUrl = _chatService.GetServerFullUrl(targetProfile);

            // URL이 비어있거나 변환에 실패했다면 기본 이미지 할당
            TargetProfile = fullUrl;

            // 컬렉션 동기화 설정 (다른 스레드에서 Add 해도 에러 안나게 ) 
            BindingOperations.EnableCollectionSynchronization(Messages, new object());

            // 2026.04.17 실시간 메세지 수신 이벤트 등록 
            //_signalRService.MessageReceived += OnMessageReceived; (2026.05.13 주석처리) 

            // ChatHub에서 JoinRoom 호출) 
            Task.Run(async () => await _signalRService.JoinRoom(RoomId));

            // 2026.03.31 
            // 비동기로 초기 메세지 로드 (Fire and Forget)
            Task.Run(async () => await LoadMessagesFromDb());

            // 2024.04.22 서버에서 SignalR로 메세지가 오면 Messenger가 이 신호를 낚아 챈다
            // 상대방이 내가 보낸 메세지를 읽었을때 
            WeakReferenceMessenger.Default.Register<NewMessageReceivedMessage>(this, (r, m) =>
            {
                // 방번호가 다른 경우 
                if (m.Message.RoomId != this.RoomId) 
                    return;

                // 내가 보낸 메시지가 다시 돌아온 경우 
                if (m.Message.SenderId == _userStore.CurrentUserEmail) 
                    return;

                // 이미 리스트에 존재하는 메시지인지 Guid로 체크 (2026.05.13) 
                if(Messages.Any(x => x.MessageGuid == m.Message.MessageGuid))
                {
                    return;
                }

                var incoming = m.Message;

                App.Current.Dispatcher.Invoke(async () =>
                {
                    // 1. 메시지 객체 생성 (현재 창 활성화 여부에 따라 IsRead 설정)
                    var newMessage = new ChatMessage
                    {
                        RoomId = incoming.RoomId,
                        SenderId = incoming.SenderId,
                        // 이름 보정: 서버에서 온 이름이 이메일이면, 생성자에서 받은 TargetName(닉네임) 사용
                        SenderName = (string.IsNullOrEmpty(incoming.SenderName) || incoming.SenderName.Contains("@"))
                                     ? TargetName : incoming.SenderName,

                        // ★ 여기도 ProfilePath가 아니라 SenderProfile로 변경!
                        SenderProfile = !string.IsNullOrEmpty(incoming.SenderProfile)
                                ? _chatService.GetServerFullUrl(incoming.SenderProfile)
                                : (TargetProfile ?? ""),

                        Message = incoming.Message,
                        MessageType = incoming.MessageType, //2026.05.08 텍스트인지 파일인지 
                        FilePath = incoming.FilePath,
                        SendTime = m.Message.SendTime,
                        IsRead = IsWindowActive, // 내가 보고 있으면 true, 아니면 false
                        IsMine = false
                    };

                    // 이미지 경로 보정
                    if (newMessage.MessageType == (int)ChatMessageTypes.Image && !string.IsNullOrEmpty(newMessage.FilePath))
                    {
                        if (!newMessage.FilePath.StartsWith("http"))
                            newMessage.FilePath = _chatService.GetServerFullUrl(newMessage.FilePath);
                    }

                    // 2. 리스트에 추가
                    Messages.Add(newMessage);

                    // -----------------------------------------------------------
                    // 🔥 [추가 항목 1] 내 로컬 DB의 채팅 목록(마지막 메시지, 안 읽은 개수) 업데이트
                    // 이 메서드를 호출해야 DB의 UnreadCount가 1 올라갑니다.
                    await _chatService.UpdateChatListAsync(
                        newMessage,
                        _targetId,
                        _targetName,
                        _userStore.CurrentUserId,
                        _userStore.CurrentUserNickname);

                    // 🔥 [추가 항목 2] 메인 화면(MainViewModel) 및 왼쪽 메뉴 뱃지 갱신 신호 발송
                    // 이 신호를 쏴줘야 목록의 주황색 숫자와 왼쪽 아이콘 숫자가 즉시 바뀝니다.
                    WeakReferenceMessenger.Default.Send(new RefreshChatListMessage());
                    // -----------------------------------------------------------

                    // [추가] 만약 내가 창을 보고 있지 않다면, MainViewModel이 배지 숫자를 새로고침 하도록 유도
                    // MainViewModel은 NewMessageReceivedMessage를 구독 중이므로, 
                    // 전체 안 읽은 개수를 다시 계산하게 됩니다.
                    if (!IsWindowActive)
                    {
                        // 사실 Messenger는 BroadCast 방식이라 이미 MainViewModel에 전달되었겠지만,
                        // 확실하게 하기 위해 DB 업데이트 이후 한 번 더 카운트 업데이트 메서드를 실행하게 유도하거나
                        // MainViewModel이 듣고 있는 다른 신호를 보낼 수도 있습니다.
                        // 일단 위에서 UpdateChatListAsync가 끝난 후 신호를 보내는 것이 안전합니다.
                    }

                    // 3. 내가 창을 보고 있다면 즉시 '읽음' 신호를 서버에 쏜다!                   
                    if (IsWindowActive)
                    {
                        try
                        {
                            // 서버 DB 업데이트
                            await _chatService.MarkAsReadAsync(RoomId, _userStore.CurrentUserId);
                            // 기로로에게 실시간 알림 전송
                            await _signalRService.SendReadReceiptAsync(RoomId, _targetId);

                            // 읽었으니 숫자를 다시 0으로 만들기 위해 목록 갱신
                            WeakReferenceMessenger.Default.Send(new RefreshChatListMessage());                            
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"실시간 읽음 처리 실패: {ex.Message}");
                        }
                    }

                    
                }, System.Windows.Threading.DispatcherPriority.Render // 즉시 그리도록 우선순위 조정 
                );
            });

            // 2026.04.22 상대방이 내가 보낸 메시지를 읽었을 때 처리
            WeakReferenceMessenger.Default.Register<MessagesReadMessage>(this, (r, m) =>
            {
                // StringComparison.OrdinalIgnoreCase를 사용하여 대소문자 구분 없이 비교
                if (string.Equals(m.RoomId, this.RoomId, StringComparison.OrdinalIgnoreCase))
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var myUnreadMessages = Messages.Where(msg => msg.IsMine && !msg.IsRead).ToList();
                        foreach (var msg in myUnreadMessages)
                        {
                            msg.IsRead = true;
                        }
                        System.Diagnostics.Debug.WriteLine($"[읽음처리성공] {m.RoomId}");
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[방번호불일치] 수신:{m.RoomId} != 내방:{this.RoomId}");
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
                await App.Current.Dispatcher.Invoke(async () =>
                {
                    Messages.Clear();
                    if (dbMessages != null)
                    {
                        foreach (var msg in dbMessages)
                        {
                            // DB에서 데이터를 가져올 때 
                            // 내 이메일과 발신자 이메일을 비교해서 IsMine 세팅 
                            msg.IsMine = (msg.SenderId == _userStore.CurrentUserId);

                            // [수정] 메세지 타입이 이미지(1)인 경우 체크
                            if (msg.MessageType == (int)ChatMessageTypes.Image || msg.MessageType == 1)
                            {
                                if (!string.IsNullOrEmpty(msg.FilePath))
                                {
                                    // 경로가 상대경로라면 풀 경로로 만들어줌
                                    if (!msg.FilePath.StartsWith("http"))
                                    {
                                        msg.FilePath = _chatService.GetServerFullUrl(msg.FilePath);
                                    }
                                }
                            }

                            Messages.Add(msg);
                        }
                    }

                    // dbMessages 중에서 "상대방이 보낸 것"이고 "아직 안 읽은 상태"인 메시지가 하나라도 있는지 체크
                    bool hasUnreadFromTarget = dbMessages?.Any(m => m.SenderId == _targetId && !m.IsRead) ?? false;

                    // 내가 읽어야 할 메세지가 있을 때만 서버에서 '읽음' 보고 
                    if (hasUnreadFromTarget)
                    {
                        // [읽음 처리 추가] 히스토리 로드 후 서버에 읽음 보고 및 상대방 알림
                        await _chatService.MarkAsReadAsync(RoomId, _userStore.CurrentUserId);
                        await _signalRService.SendReadReceiptAsync(RoomId, _targetId);
                        WeakReferenceMessenger.Default.Send(new RefreshChatListMessage());
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
            if (string.IsNullOrWhiteSpace(InputMessage))
                return;

            // 가상의 전송 로직 (나중에 SignalR/API 연동 부분) 
            var myMsg = new ChatMessage
            {
                MessageGuid = Guid.NewGuid().ToString(),
                RoomId = RoomId,
                SenderId = _userStore.CurrentUserId,
                SenderName = _userStore.CurrentUserNickname,
                Message = InputMessage,
                SendTime = DateTime.Now,
                IsMine = true,
                IsRead = false,     // DB 컬럼 대응 
                MessageType = 0,    // 기본 텍스트 타입 
                SendState = (int)ChatMessage.MessageStatus.Success  // 
            };

            // UI에 즉시 추가 
            Messages.Add(myMsg);
            string currentInput = InputMessage;
            // 입력창 비우기 
            InputMessage = string.Empty;

            // 서버 전송 및 DB 저장 로직 수행 
            try
            {
               
                await _signalRService.SendMessageAsync(RoomId, TargetId, currentInput);
                await _chatService.UpdateChatListAsync(myMsg, TargetId, TargetName, _userStore.CurrentUserId, _userStore.CurrentUserNickname);                

                WeakReferenceMessenger.Default.Send(new RefreshChatListMessage());

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"전송 에러 : {ex.Message}");

                // 네트워크 단절 등 예외 발생 시 상태를 '실패'로 변경 
                myMsg.SendState = (int)ChatMessage.MessageStatus.Fail;
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

        private async Task SendFileMessageAsync(string originFileName, string localFilePath)
        {
            // 메세지 타입 설정 
            int initialMessageType = GetInitialMessageType(originFileName);

            // 1. [UI 즉시 반영] 전송 중 상태로 목록에 먼저 추가
            var myMsg = new ChatMessage
            {
                MessageGuid = Guid.NewGuid().ToString(),
                RoomId = RoomId,
                SenderId = _userStore.CurrentUserId,
                SenderName = _userStore.CurrentUserNickname,
                SenderProfile = _userStore.CurrentUserProfile,  // 내 프로필 추가 
                Message = "파일을 보내는 중....",
                MessageType = initialMessageType,
                FileName = originFileName,
                FilePath = localFilePath, // UI 선반영용 로컬 경로
                SendTime = DateTime.Now,
                IsMine = true,
                SendState = (int)ChatMessage.MessageStatus.Sending
            };

            // UI 스레드에서 안전하게 추가
            App.Current.Dispatcher.Invoke(() => Messages.Add(myMsg));

            try
            {
                // 2. 서버 파일 업로드 및 타입 정보 수신
                // ChatService.UploadFileAsync가 이제 url뿐만 아니라 messageType도 반환하도록 수정되었다고 가정합니다.
                // 만약 string만 반환한다면, 아래 주석처럼 DTO를 받도록 ChatService를 살짝 수정해야 합니다.
                var uploadResult = await _chatService.UploadFileExtendedAsync(localFilePath);

                if (uploadResult == null || string.IsNullOrEmpty(uploadResult.Url))
                {
                    throw new Exception("서버 업로드 실패");
                }

                string fullUrl = _chatService.GetServerFullUrl(uploadResult.Url);
                int finalMessageType = uploadResult.MessageType; // 서버가 결정해준 타입 (이미지/비디오/파일)

                // 3. UI 객체 정보 업데이트
                myMsg.MessageType = finalMessageType;
                myMsg.FilePath = fullUrl;
                myMsg.Message = GetMessageByTypeName(finalMessageType); // 타입에 따른 문구 자동 세팅

                // 4. DB 저장용 객체 생성
                var dbSaveMsg = new ChatMessage
                {
                    RoomId = myMsg.RoomId,
                    SenderId = myMsg.SenderId,
                    SenderName = myMsg.SenderName,
                    SenderProfile = myMsg.SenderProfile,
                    Message = myMsg.Message,
                    MessageType = finalMessageType,
                    FilePath = fullUrl,
                    FileName = originFileName,
                    SendTime = myMsg.SendTime,
                    MessageGuid = myMsg.MessageGuid,
                    IsRead = false
                };

                // DB 저장 시도
                bool isSaved = await _chatService.SaveMessageAsync(dbSaveMsg);

                if (isSaved)
                {
                    myMsg.SendState = (int)ChatMessage.MessageStatus.Success;

                    // SignalR용 DTO 생성
                    var msgDto = new CommonLib.Dtos.ChatMessageDto
                    {
                        MessageGuid = myMsg.MessageGuid,
                        RoomId = RoomId,
                        SenderId = _userStore.CurrentUserId,
                        SenderName = _userStore.CurrentUserNickname,
                        Message = myMsg.Message,
                        MessageType = finalMessageType,
                        FilePath = fullUrl,
                        FileName = originFileName,
                        SendTime = myMsg.SendTime
                    };

                    await _signalRService.SendDtoMessageAsync(msgDto, TargetId);
                    await _chatService.UpdateChatListAsync(myMsg, TargetId, TargetName, _userStore.CurrentUserId, _userStore.CurrentUserNickname);
                }
                else
                {
                    myMsg.SendState = (int)ChatMessage.MessageStatus.Fail;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[파일 전송 에러] {ex.Message}");
                myMsg.SendState = (int)ChatMessage.MessageStatus.Fail;
                myMsg.Message = "전송 실패";
            }

        }

        /// <summary>
        /// 메시지 타입(이미지, 비디오, 파일 등)에 따라 UI에 표시될 요약 문구를 반환합니다.
        /// </summary>
        private string GetMessageByTypeName(int type)
        {
            // CommonLib.Enums.ChatMessageTypes 사용
            return (ChatMessageTypes)type switch
            {
                ChatMessageTypes.Image => "사진을 보냈습니다.",
                ChatMessageTypes.Video => "동영상을 보냈습니다.",
                ChatMessageTypes.File => "파일을 보냈습니다.",
                _ => "메시지를 보냈습니다." // 기본값 (Text 등)
            };
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
                    // 내가 채팅창을 바라고 보고 있는 경우 UI 업데이트 
                    if (IsWindowActive)
                    {
                        // 이미 리스트에 있는 상대방 메세지들 읽음 처리  
                        foreach (var msg in Messages.Where(m => !m.IsMine && !m.IsRead))
                        {
                            msg.IsRead = true;
                        }

                        try
                        {
                            // 서버에 내가 메세지 읽었음을 알림 (상대방 1도 사라지게 함) 

                            await _chatService.MarkAsReadAsync(RoomId, _userStore.CurrentUserId);
                            await _signalRService.SendReadReceiptAsync(RoomId, _targetId);
                            WeakReferenceMessenger.Default.Send(new RefreshChatListMessage());
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"읽음 처리 중 에러: {ex.Message}");
                        }
                    }
                });
            }
        }

        // 창이 켜질 때 한꺼번에 읽음 처리하기 위한 메서드 
        public async Task MarkAllReadAsync()
        {
            try
            {
                // UI에서 상대방 메세지만 즉시 읽음 처리 
                App.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var msg in Messages.Where(m => !m.IsMine && !m.IsRead))
                    {
                        msg.IsRead = true;
                    }
                });

                // _chatService와 _signalRService를 사용하여 읽음 처리 진행 
                await _chatService.MarkAsReadAsync(RoomId, _userStore.CurrentUserId);
                await _signalRService.SendReadReceiptAsync(RoomId, _targetId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MarkAllRead 에러: {ex.Message}");
            }

        }

        // 2026.04.25 Add 
        // [메세지 전송 재시도 커맨드] 
        [RelayCommand]
        private async Task RetrySendMessage(ChatMessage failedMsg)
        {
            if (failedMsg == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(failedMsg.MessageGuid))
            {
                failedMsg.MessageGuid = Guid.NewGuid().ToString();
            }

            // 상태를 다시 '성공' 혹은 '전송 중'으로 변경 (UI에서 실패 문구 사라짐) 
            failedMsg.SendState = (int)ChatMessage.MessageStatus.Success;

            try
            {
                // 서버 저장 시도 
                bool saveSuccess = await _chatService.SaveMessageAsync(failedMsg);

                if (saveSuccess)
                {
                    // 리스트 업데이트 및 SignalR 전송 
                    await _chatService.UpdateChatListAsync(failedMsg, TargetId, TargetName, _userStore.CurrentUserId, _userStore.CurrentUserNickname);

                    // 메세지 타입에 따른 분기 처리 
                    if (failedMsg.MessageType == (int)ChatMessageTypes.Image)
                    {
                        // SignalR용 DTO 생성 및 전송
                        var msgDto = new CommonLib.Dtos.ChatMessageDto
                        {
                            MessageGuid = failedMsg.MessageGuid,
                            RoomId = RoomId,
                            SenderId = failedMsg.SenderId,
                            SenderName = failedMsg.SenderName,
                            Message = failedMsg.Message,
                            MessageType = failedMsg.MessageType,
                            FilePath = failedMsg.FilePath, // 전체 경로
                            FileName = failedMsg.FileName,
                            SendTime = failedMsg.SendTime
                        };
                        await _signalRService.SendDtoMessageAsync(msgDto, TargetId);
                    }
                    else
                    {
                        await _signalRService.SendMessageAsync(RoomId, TargetId, failedMsg.Message);
                    }
                }
                else
                {
                    failedMsg.SendState = (int)ChatMessage.MessageStatus.Fail;
                }
            }
            catch (Exception ex)
            {
                failedMsg.SendState = (int)ChatMessage.MessageStatus.Fail;
            }
        }

        // 삭제 커맨드 (실패한 메시지가 보기 싫은 경우) 
        [RelayCommand]
        private void DeleteMessage(ChatMessage msg)
        {
            if (msg != null && Messages.Contains(msg))
            {
                Messages.Remove(msg);

            }
        }

        // 2026.04.29 
        // 서버(SignalR)로부터 인원 변경 알림을 받았을 때 호출할 메서드 
        public void UPdateParticipantCount(int count)
        {
            // UI 스레드에서 안전하게 업데이트 
            App.Current.Dispatcher.Invoke(() =>
            {
                ParticipantCount = count;
            });
        }

        // 2026.05.12 Add (첨부하는 파일에 따라서 ChatMessageType 이 변경 
        // 파일 확장자를 분석하여 적절한 메세지 타입을 반환 
        private int GetInitialMessageType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();

            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => (int)ChatMessageTypes.Image,
                ".mp4" or ".avi" or ".mov" => (int)ChatMessageTypes.Video,
                _ => (int)ChatMessageTypes.File // 그 외 모든 것은 일반 파일
            };
        }
    }
}

