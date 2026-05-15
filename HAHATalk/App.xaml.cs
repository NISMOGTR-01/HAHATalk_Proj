
using HAHATalk.Services;
using HAHATalk.Stores;
using HAHATalk.ViewModels;
using HAHATalk.Views;
using Microsoft.Extensions.DependencyInjection;
using System; // Uri를 사용하기 위함
using System.Configuration;
using System.Data;
using System.Net.Http; // HttpClient 관련 설정
using System.Windows;
using Microsoft.Extensions.Configuration;
using System.IO;


namespace HAHATalk
{
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ChatHubUrl { get; set; } = string.Empty;
    }


    public partial class App : Application
    {
        public App()
        {
            Services = ConfigureServices();
            Startup += App_Startup;          
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            // 생성자 주입 
            var mainView = App.Current.Services.GetService<MainView>()!;
            mainView.Show();
        }

        public new static App Current => (App)Application.Current;

    
        public IServiceProvider Services { get; }

        // 서비스 등록 및 설정 
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            // 1. 설정 빌드 (appsettings.json 읽기)
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // 2. JSON의 "ApiSettings" 섹션을 객체로 매핑
            // .Get<ApiSettings>()가 null을 반환할 수 있으므로 기본값 객체를 생성해둡니다.
            var apiSettings = config.GetSection("ApiSettings").Get<ApiSettings>() ?? new ApiSettings();

            // 3. 주소값 검증 (하드코딩 대신 설정 파일이 비어있으면 경고를 띄웁니다)
            if (string.IsNullOrEmpty(apiSettings.BaseUrl))
            {
                // 실무에서는 여기서 로그를 남기거나 실행을 중단할 수 있습니다.
                // 일단은 빈 값이면 Uri 생성 시 에러가 나므로 체크가 필요합니다.
                MessageBox.Show("설정 파일(appsettings.json)에서 BaseUrl을 찾을 수 없습니다!", "설정 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // 💡 2. 설정 객체 및 Store 등록 (이게 핵심!)
            services.AddSingleton(apiSettings); // ApiSettings를 주입할 수 있게 등록
            services.AddSingleton<MainNavigationStore>(); // 🚩 에러의 주범! 싱글톤으로 등록
            services.AddSingleton<UserStore>(); // 로그인 정보 저장소도 싱글톤!

            services.AddHttpClient();

            services.AddHttpClient<IAccountService, AccountService>(client =>
            {
                client.BaseAddress = new Uri(apiSettings.BaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            services.AddHttpClient<IFriendService, FriendService>(client =>
            {
                client.BaseAddress = new Uri(apiSettings.BaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            services.AddHttpClient<IChatService, ChatService>(client =>
            {
                client.BaseAddress = new Uri(apiSettings.BaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
      
            services.AddSingleton<ISignalRService, SignalRService>();


            // 💡 3. Services 등록
            services.AddSingleton<INavigationService, NavigationService>();
            

            services.AddSingleton<IWindowManager, WindowManager>();

            // ViewModels 
            services.AddSingleton<MainViewModel>();
            services.AddTransient<LoginControlViewModel>();
            services.AddTransient<SignupControlViewModel>();
            services.AddTransient<ChangePwdControlViewModel>();
            services.AddTransient<FindAccountControlViewModel>();
            services.AddTransient<MainNaviControlViewModel>();
            services.AddTransient<LockScreenViewModel>();
            services.AddTransient<SettingsViewModel>();


            services.AddSingleton<FriendListControlViewModel>();
            services.AddSingleton<ChatListControlViewModel>();



            // Views 
            services.AddSingleton(s => new MainView()
            {
                DataContext = s.GetRequiredService<MainViewModel>()
            });


            return services.BuildServiceProvider();
        }
    }

}
