using HAHATalk.Server.Hubs;
using Microsoft.EntityFrameworkCore;
using HAHATalk.Server.Data;
using HAHATalk.Server.Repository;
using HAHATalk.Server.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// DB 연결 문자열 등록 (2026.03.26) 
var connectionString = builder.Configuration.GetConnectionString("MSAccountDb");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));




builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IFriendRepository, FriendRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();

// 1. Signal R 서비스를 시스템에 등록 
builder.Services.AddSignalR();


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build(); // [추가] 설정을 바탕으로 앱 빌드

// 2. 미들웨어 설정 (요청이 들어오는 통로 설정)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // 개발 환경에서 API 문서 활성화
}

//app.UseHttpsRedirection();
app.UseAuthorization();

// 3. 엔드포인트 맵핑 (길 찾기 설정)
app.MapControllers();
app.MapHub<ChatHub>("/ChatHub"); // SignalR 허브 연결

// 4. 서버 시작 (가장 중요!)
app.Run(); // [추가] 이 코드가 있어야 서버가 종료되지 않고 계속 대기합니다.