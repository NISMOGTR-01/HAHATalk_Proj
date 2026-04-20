
using HAHATalk.Services;
using HAHATalk.Stores;
using HAHATalk.ViewModels;
using HAHATalk.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;
using System; // Uri를 사용하기 위함
using System.Net.Http; // HttpClient 관련 설정


namespace HAHATalk
{

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

            //stores
            services.AddSingleton<MainNavigationStore>();
            services.AddSingleton<UserStore>();         // 2026.03.17 Add UserStore

            // services
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IWindowManager, WindowManager>();
            // 2026.03.26 Signal R Service Add
            services.AddSingleton<ISignalRService, SignalRService>();

            // Repositories
            //services.AddTransient<IFriendRepository, FriendRepository>();

            // AccountService
            services.AddHttpClient<IAccountService, AccountService>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:7203");
            });

            // ChatService
            services.AddHttpClient<IChatService, ChatService>(client =>
            {
                // 서버 주소 확인 
                client.BaseAddress = new Uri("https://localhost:7203");
            });

            // FriendService
            services.AddHttpClient<IFriendService, FriendService>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:7203");
            });

            // [추가] 만약 나중에 HTTPS(7203)를 사용할 때 인증서 에러가 난다면 
            // 아래와 같이 ConfigurePrimaryHttpMessageHandler를 추가하여 해결할 수 있습니다.
            /*
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
            });
            */

            //services.AddSingleton<ITestService, TestService>();

            // ViewModels 
            services.AddTransient<MainViewModel>();
            services.AddTransient<LoginControlViewModel>();
            services.AddTransient<SignupControlViewModel>();
            services.AddTransient<ChangePwdControlViewModel>();
            services.AddTransient<FindAccountControlViewModel>();
            services.AddTransient<MainNaviControlViewModel>();

            // 2026.03.17 FriendListControlViewModel 추가 
            services.AddTransient<FriendListControlViewModel>();
            // 2026.03.17 ChatListControlViewModel 추가 
            services.AddTransient<ChatListControlViewModel>();

            // Views 
            services.AddSingleton(s => new MainView()
            {
                DataContext = s.GetRequiredService<MainViewModel>()
            });
            //services.AddSingleton<MainView>();


            return services.BuildServiceProvider();
        }
    }

}
