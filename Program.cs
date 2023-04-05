using System.Reactive.Linq;
using FoxSayQ.utils;
using Manganese.Text;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions;
using Newtonsoft.Json;

namespace FoxSayQ
{
  public class Program
  {
    public static MainConfig main_config = Config.ReadConfig<MainConfig>("configs/main_config.json");

    public static void Main_(){
      Console.WriteLine(main_config.robotConfig.mirai_address);
    }
    public static async Task Main()
    {
      var bot = new MiraiBot
      {
        Address = main_config.robotConfig.mirai_address,
        QQ = main_config.robotConfig.robot_qq_num,
        VerifyKey = main_config.robotConfig.mirai_vertify
      };

      // 注意: `LaunchAsync`是一个异步方法，请确保`Main`方法的返回值为`Task`
      await bot.LaunchAsync();

      HandleMessage handleMessage = new HandleMessage(main_config.robotConfig.robot_qq_num);

      //群组事件监听
      bot.MessageReceived
          .OfType<GroupMessageReceiver>()
          .Subscribe(x =>
          {
            handleMessage.HandleGroupMessage(x);
          });

      //好友事件监听
      bot.MessageReceived
          .OfType<FriendMessageReceiver>()
          .Subscribe(x =>
          {
            Console.WriteLine($"收到了来自好友{x.FriendId}发送的消息：{x.MessageChain.GetPlainMessage()}");
          });

      //陌生人事件监听
      bot.MessageReceived
          .OfType<StrangerMessageReceiver>()
          .Subscribe(x =>
          {
            Console.WriteLine($"收到了来自陌生人{x.StrangerId}发送的消息：{x.MessageChain.GetPlainMessage()}");
          });
      
      Console.WriteLine("机器人已就绪");

      //卡住主线程，让子线程循环
      while (true)
      {
        string? command = Console.ReadLine();
        if (command == null)
        {
          continue;
        }
        else if (command == "exit" || command == "e")
        {
          Console.WriteLine("Bye");
          break;
        }
        else if (command == "add")
        {
          string? group = Console.ReadLine();
        }
      }
    }
  }
}